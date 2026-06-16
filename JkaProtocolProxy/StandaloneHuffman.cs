// JKHuffman.cs
// Standalone adaptive Huffman compress / decompress for Jedi Knight / Quake III packets.
//
// Extracted and adapted from entdark/JKClient (GPL-2.0):
//   https://github.com/entdark/JKClient/blob/master/JKClient/Huffman.cs
//
// The adaptive-tree algorithm is a direct C# port of the id Software / Q3 Huffman
// implementation (originally in huffman.c / huffman.cpp).
//
// Usage:
//   byte[] compressed = JKHuffman.Compress(rawBytes);
//   byte[] decompressed = JKHuffman.Decompress(compressed);
//
// Wire format (identical to the engine's MSG_Compress / MSG_Decompress):
//   byte[0]  = (originalLength >> 8) & 0xFF   (high byte)
//   byte[1]  =  originalLength & 0xFF          (low  byte)
//   byte[2…] = Huffman bit-stream (LSB-first, padded to a full byte at end)

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Standalone adaptive Huffman codec compatible with Jedi Knight: Jedi Academy /
/// Jedi Knight II / Quake III Arena network packets.
/// </summary>
public static unsafe class JKHuffman
{
    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Compresses <paramref name="data"/> using the adaptive Huffman algorithm
    /// used by the JK / Q3 engine (MSG_Compress).
    /// </summary>
    public static byte[] Compress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        int size = data.Length;
        if (size == 0) return Array.Empty<byte>();

        // Allocate worst-case output (novel symbol = 9 bits, so ~size*2 + header)
        byte[] seq = new byte[2 + size * 2 + 4];

        // Header: original length big-endian
        seq[0] = (byte)(size >> 8);
        seq[1] = (byte)(size & 0xFF);

        var huff = new HuffState();
        huff.Init(compressor: true);
        huff.bloc = 16; // start writing bits after the 2-byte header

        for (int i = 0; i < size; i++)
        {
            int ch = data[i];
            huff.Transmit(ch, seq);
            huff.AddReference((byte)ch);
        }

        huff.bloc += 8; // pad to full byte + guard
        int outLen = huff.bloc >> 3;

        huff.Free();

        byte[] result = new byte[outLen];
        Buffer.BlockCopy(seq, 0, result, 0, outLen);
        return result;
    }

    /// <summary>
    /// Decompresses a byte array previously produced by <see cref="Compress"/>
    /// or by the JK / Q3 engine (MSG_Decompress equivalent).
    /// </summary>
    public static byte[] Decompress(byte[] compressed, int originalSize, int startBit = 0)
    {
        if (compressed == null) throw new ArgumentNullException(nameof(compressed));
        if (originalSize == 0) return Array.Empty<byte>();

        byte[] output = new byte[originalSize];

        var huff = new HuffState();
        huff.Init(compressor: false);
        int offset = startBit;

        for (int i = 0; i < originalSize; i++)
        {
            int ch = 0;
            huff.OffsetReceive(ref ch, compressed, ref offset);

            if (ch == NYT)
            {
                ch = 0;
                for (int bit = 7; bit >= 0; bit--)
                    ch |= huff.GetBit(compressed, ref offset) << bit;
            }

            output[i] = (byte)ch;
            huff.AddReference((byte)ch);
        }

        huff.Free();
        return output;
    }

    // -------------------------------------------------------------------------
    // Constants (mirror the engine)
    // -------------------------------------------------------------------------
    private const int HMAX = 256;           // alphabet size
    private const int NYT = HMAX;           // Not-Yet-Transmitted sentinel
    private const int INTERNAL_NODE = HMAX + 1;
    private const int NODES_MAX = HMAX * 3;

    // -------------------------------------------------------------------------
    // Node struct (mirrors engine's nodetype)
    // -------------------------------------------------------------------------
    private struct Node
    {
        public Node* left, right, parent;
        public Node* next, prev;
        public Node** head;
        public int weight;
        public int symbol;
    }

    // -------------------------------------------------------------------------
    // HuffState — the adaptive Huffman tree plus its helpers
    // (mirrors the Huffman class from JKClient without the IDisposable overhead)
    // -------------------------------------------------------------------------
    private struct HuffState
    {
        public int bloc;       // current bit offset (compress or decompress)

        private int _blocNode;
        private int _blocPtrs;

        private Node* _tree;
        private Node* _lhead;
        // _ltail used only in decompressor; keeping the field avoids #if branches
        private Node* _ltail;
        private Node** _freelist;

        // Unmanaged arenas
        private Node** _loc;
        private Node* _nodeList;
        private Node** _nodePtrs;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------
        public void Init(bool compressor)
        {
            _loc = (Node**)Marshal.AllocHGlobal(sizeof(Node*) * (HMAX + 1));
            _nodeList = (Node*)Marshal.AllocHGlobal(sizeof(Node) * NODES_MAX);
            _nodePtrs = (Node**)Marshal.AllocHGlobal(sizeof(Node*) * NODES_MAX);

            Memset(_loc, 0, sizeof(Node*) * (HMAX + 1));
            Memset(_nodeList, 0, sizeof(Node) * NODES_MAX);
            Memset(_nodePtrs, 0, sizeof(Node*) * NODES_MAX);

            _freelist = null;
            _blocNode = 0;
            _blocPtrs = 0;

            _tree = _lhead = _loc[NYT] = &_nodeList[_blocNode++];
            if (!compressor)
                _ltail = _tree;

            _tree->symbol = NYT;
            _tree->weight = 0;
            _lhead->next = _lhead->prev = null;
            _tree->parent = _tree->left = _tree->right = null;

            if (compressor)
                _loc[NYT] = _tree;
        }

        public void Free()
        {
            Marshal.FreeHGlobal((IntPtr)_loc);
            Marshal.FreeHGlobal((IntPtr)_nodeList);
            Marshal.FreeHGlobal((IntPtr)_nodePtrs);
        }

        // ------------------------------------------------------------------
        // Compress path
        // ------------------------------------------------------------------

        /// <summary>Encodes one symbol into <paramref name="fout"/> at <see cref="bloc"/>.</summary>
        public void Transmit(int ch, byte[] fout)
        {
            if (_loc[ch] == null)
            {
                // Novel: send NYT codeword, then the 8 raw bits MSB-first
                Transmit(NYT, fout);
                for (int i = 7; i >= 0; i--)
                    AddBit((sbyte)((ch >> i) & 1), fout);
            }
            else
            {
                Send(_loc[ch], null, fout);
            }
        }

        private void AddBit(sbyte bit, byte[] fout)
        {
            if ((bloc & 7) == 0)
                fout[bloc >> 3] = 0;
            fout[bloc >> 3] |= (byte)(bit << (bloc & 7));
            bloc++;
        }

        private void Send(Node* node, Node* child, byte[] fout)
        {
            if (node->parent != null)
                Send(node->parent, node, fout);
            if (child != null)
                AddBit(node->right == child ? (sbyte)1 : (sbyte)0, fout);
        }

        // ------------------------------------------------------------------
        // Decompress path
        // ------------------------------------------------------------------

        /// <summary>
        /// Walks the tree from <paramref name="fin"/> starting at bit
        /// <paramref name="offset"/> and returns the decoded symbol in
        /// <paramref name="ch"/> (may be <see cref="NYT"/>).
        /// </summary>
        public void OffsetReceive(ref int ch, byte[] fin, ref int offset)
        {
            Node* node = _tree;
            bloc = offset;
            while (node != null && node->symbol == INTERNAL_NODE)
                node = ReadBit(fin) != 0 ? node->right : node->left;

            if (node == null) { ch = 0; return; }
            ch = node->symbol;
            offset = bloc;
        }

        /// <summary>Reads a single bit at <paramref name="offset"/> (public for NYT raw-byte read).</summary>
        public int GetBit(byte[] fin, ref int offset)
        {
            bloc = offset;
            int t = (fin[bloc >> 3] >> (bloc & 7)) & 1;
            bloc++;
            offset = bloc;
            return t;
        }

        // Internal bit reader (advances bloc without updating offset)
        private int ReadBit(byte[] fin)
        {
            int t = (fin[bloc >> 3] >> (bloc & 7)) & 1;
            bloc++;
            return t;
        }

        // ------------------------------------------------------------------
        // Shared adaptive-tree update (called after each symbol by both sides)
        // ------------------------------------------------------------------

        public void AddReference(byte ch)
        {
            if (_loc[ch] == null)
            {
                // First occurrence of this symbol: split the NYT node
                Node* tnode = &_nodeList[_blocNode++];  // new leaf for ch
                Node* tnode2 = &_nodeList[_blocNode++];  // new internal parent

                tnode2->symbol = INTERNAL_NODE;
                tnode2->weight = 1;
                tnode2->next = _lhead->next;

                if (_lhead->next != null)
                {
                    _lhead->next->prev = tnode2;
                    tnode2->head = _lhead->next->weight == 1
                        ? _lhead->next->head
                        : GetPPNode();
                    if (tnode2->head != _lhead->next->head)
                        *tnode2->head = tnode2;
                }
                else
                {
                    tnode2->head = GetPPNode();
                    *tnode2->head = tnode2;
                }

                _lhead->next = tnode2;
                tnode2->prev = _lhead;

                tnode->symbol = ch;
                tnode->weight = 1;
                tnode->next = _lhead->next;

                if (_lhead->next != null)
                {
                    _lhead->next->prev = tnode;
                    if (_lhead->next->weight == 1)
                    {
                        tnode->head = _lhead->next->head;
                    }
                    else
                    {
                        tnode->head = GetPPNode();
                        *tnode->head = tnode2;
                    }
                }
                else
                {
                    tnode->head = GetPPNode();
                    *tnode->head = tnode;
                }

                _lhead->next = tnode;
                tnode->prev = _lhead;
                tnode->left = tnode->right = null;

                if (_lhead->parent != null)
                {
                    if (_lhead->parent->left == _lhead) _lhead->parent->left = tnode2;
                    else _lhead->parent->right = tnode2;
                }
                else
                {
                    _tree = tnode2;
                }

                tnode2->right = tnode;
                tnode2->left = _lhead;
                tnode2->parent = _lhead->parent;
                _lhead->parent = tnode->parent = tnode2;
                _loc[ch] = tnode;

                Increment(tnode2->parent);
            }
            else
            {
                Increment(_loc[ch]);
            }
        }

        private void Increment(Node* node)
        {
            if (node == null) return;

            if (node->next != null && node->next->weight == node->weight)
            {
                Node* lnode = *node->head;
                if (lnode != node->parent)
                    Swap(lnode, node);
                Swaplist(lnode, node);
            }

            if (node->prev != null && node->prev->weight == node->weight)
                *node->head = node->prev;
            else
            {
                *node->head = null;
                FreePPNode(node->head);
            }

            node->weight++;

            if (node->next != null && node->next->weight == node->weight)
                node->head = node->next->head;
            else
            {
                node->head = GetPPNode();
                *node->head = node;
            }

            if (node->parent != null)
            {
                Increment(node->parent);
                if (node->prev == node->parent)
                {
                    Swaplist(node, node->parent);
                    if (*node->head == node)
                        *node->head = node->parent;
                }
            }
        }

        private void Swap(Node* n1, Node* n2)
        {
            Node* p1 = n1->parent, p2 = n2->parent;
            if (p1 != null) { if (p1->left == n1) p1->left = n2; else p1->right = n2; } else _tree = n2;
            if (p2 != null) { if (p2->left == n2) p2->left = n1; else p2->right = n1; } else _tree = n1;
            n1->parent = p2;
            n2->parent = p1;
        }

        private static void Swaplist(Node* n1, Node* n2)
        {
            Node* tmp;
            tmp = n1->next; n1->next = n2->next; n2->next = tmp;
            tmp = n1->prev; n1->prev = n2->prev; n2->prev = tmp;
            if (n1->next == n1) n1->next = n2;
            if (n2->next == n2) n2->next = n1;
            if (n1->next != null) n1->next->prev = n1;
            if (n2->next != null) n2->next->prev = n2;
            if (n1->prev != null) n1->prev->next = n1;
            if (n2->prev != null) n2->prev->next = n2;
        }

        // ------------------------------------------------------------------
        // PPNode pool (mirrors GetPPNode / FreePPNode exactly)
        // ------------------------------------------------------------------

        private Node** GetPPNode()
        {
            if (_freelist == null)
                return &_nodePtrs[_blocPtrs++];

            Node** tpp = _freelist;
            _freelist = (Node**)*tpp;
            return tpp;
        }

        private void FreePPNode(Node** ppnode)
        {
            *ppnode = (Node*)_freelist;
            _freelist = ppnode;
        }

        // ------------------------------------------------------------------
        // Utility
        // ------------------------------------------------------------------
        private static void Memset(void* ptr, byte val, int count)
        {
            byte* b = (byte*)ptr;
            for (int i = 0; i < count; i++) b[i] = val;
        }
    }
}