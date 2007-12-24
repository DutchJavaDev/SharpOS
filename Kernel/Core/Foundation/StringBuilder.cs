// 
// (C) 2007 The SharpOS Project Team (http://sharpos.sourceforge.net)
//
// Authors:
//	William Lahti <xfurious@gmail.com>
//  Bruce Markham <illuminus86@gmail.com>
//
// Licensed under the terms of the GNU GPL v3,
//  with Classpath Linking Exception for Libraries
//

using System;

using SharpOS.Memory;
using SharpOS.ADC;

namespace SharpOS.Foundation {
	public unsafe struct StringBuilder {
        internal CString8* buffer;
        private uint capacity;
        private uint caret;

        public const uint DefaultCapacity = 127;

        private void Write_INTERNAL(byte* ptr, uint count)
        {
            byte* oPtr = buffer->Pointer;
            uint caretAdvance = count;
            for (int x = 0; x < count; x++)
            {
                if (ptr[x] == (byte)'\0')
                {
                    caretAdvance = (uint)x + 1;
                    break;
                }
                oPtr[caret + x] = ptr[x];
            }
            caret += caretAdvance;

            Tie();
        }
        private void Write_INTERNAL(string str)
        {
            byte* oPtr = buffer->Pointer;
            for (int x = 0; x < str.Length; x++)
            {
                oPtr[caret + x] = (byte)str[x];
            }
            caret += (uint)str.Length;

            Tie();
        }
        private void Write_INTERNAL(byte ch)
        {
            buffer->Pointer[caret] = ch;
            caret = caret + 1;

            Tie();
        }

        public void AppendChar(byte ch)
		{
            uint thisLength = (uint)this.Length;
            EnsureCapacity(thisLength + 1);

            Write_INTERNAL(ch);
		}
        public void Append(string str)
        {
            uint strLength = (uint)str.Length;
            uint thisLength = (uint)this.Length;
            EnsureCapacity(thisLength + strLength);

            Write_INTERNAL(str);
        }

		public void Append (byte *str)
		{
			uint strLength = (uint)ByteString.Length (str);
            uint thisLength = (uint)this.Length;
            EnsureCapacity(strLength + thisLength);

            Write_INTERNAL(str, strLength);
		}
        public void Append(CString8* str)
        {
            Append((byte*)str->Pointer);
        }

		public void AppendNumber (int number)
		{
			AppendNumber (number, false);
		}
		
		public void AppendNumber (int number, bool hex)
		{
            byte* str = (byte*)Convert.ToString(number, hex);
            Append(str);
            ADC.MemoryManager.Free((void*)str);
		}

		public void AppendNumber (short number, bool hex)
		{
			AppendNumber ((int)number, hex);
		}
		
		public void AppendNumber (short number)
		{
			AppendNumber ((int)number, false);
		}

		public void AppendNumber (byte number, bool hex)
		{
			AppendNumber ((int)number, hex);
		}
		
		public void Clear ()
		{
            caret = 0;
			Tie ();
		}

        private void Tie()
        {
            buffer->Pointer[caret] = (byte)'\0';
        }

        public int Length
        {
            get
            {
                return this.buffer->Length;
            }
        }

        public void RemoveAt(int startIndex, int count)
        {
            if (count == 0)
                return;

            int length = this.Length;
            Diagnostics.Assert(startIndex >= 0, "StringBuilder::Remove(int,int): Parameter 'startIndex' is out of range");
            Diagnostics.Assert(startIndex < length, "StringBuilder::Remove(int,int): Parameter 'startIndex' is out of range");
            Diagnostics.Assert(count >= 0, "StringBuilder::Remove(int,int): Parameter 'count' is out of range");
            Diagnostics.Assert((startIndex + count) <= length, "StringBuilder::Remove(int,int): Parameter 'count' is out of range");

            int partA_first = 0;
            int partA_last = startIndex - 1;

            int partB_first = partA_last + count + 1;
            int partB_last = this.Length - 1;
            int partB_length = (partB_last - partB_first) + 1;

            for (int x = 0; x < partB_length; x++)
            {
                this.buffer->Pointer[partA_last + x + 1] =
                    this.buffer->Pointer[partB_first + x];
            }

            caret = (uint)(partA_last + partB_length + 1);
            Tie();
        }

        private void Expand(uint amount)
        {
            StringBuilder* tempInstance = CREATE(this.buffer, amount);
            this.buffer = tempInstance->buffer;
            this.capacity = tempInstance->capacity;
            ADC.MemoryManager.Free(tempInstance);
        }

        public uint EnsureCapacity(uint minimumCapacity)
        {
            uint thisLength = (uint)this.Length;

            if (minimumCapacity > this.capacity)
            {
                uint amountToExpand =
                    (((minimumCapacity - this.capacity) / DefaultCapacity) + 1)
                        * DefaultCapacity;

                this.Expand(amountToExpand);
            }

            return this.capacity;
        }
        
        public static StringBuilder* CREATE()
        {
            return CREATE(DefaultCapacity);
        }

        public static StringBuilder* CREATE(CString8* initialContents)
        {
            Diagnostics.Assert(initialContents != null, "StringBuilder::Create(CString8*): Parameter 'initialContents' should not be null");
            return CREATE(initialContents, DefaultCapacity);
        }

        public static StringBuilder* CREATE(uint initialCapacity)
        {
            StringBuilder* instance = (StringBuilder*)ADC.MemoryManager.Allocate((uint)sizeof(StringBuilder));
            
            instance->buffer = (CString8*)ADC.MemoryManager.Allocate(initialCapacity + 1);
            instance->capacity = initialCapacity;

            byte* ptr = instance->buffer->Pointer;
            for (int x = 0; x < (initialCapacity + 1); x++)
                ptr[x] = (byte)'\0';

            instance->caret = 0;

            return instance;
        }

        public static StringBuilder* CREATE(CString8* initialContents, uint additionalPadding)
        {
            Diagnostics.Assert(initialContents != null, "StringBuilder::Create(CString8*,uint): Parameter 'initialContents' should not be null");

            StringBuilder* instance = (StringBuilder*)ADC.MemoryManager.Allocate((uint)sizeof(StringBuilder));
            uint initialCapacity = (uint)initialContents->Length + additionalPadding;

            instance->buffer = (CString8*)ADC.MemoryManager.Allocate(initialCapacity + 1);
            instance->capacity = initialCapacity;

            byte* oPtr = instance->buffer->Pointer;
            byte* iPtr = initialContents->Pointer;

            bool inFill = false;
            for (int x = 0; x < (instance->capacity); x++)
            {
                if (!inFill && iPtr[x] == (byte)'\0')
                    inFill = true;
                if (inFill)
                    oPtr[x] = (byte)'\0';
                else
                {
                    oPtr[x] = iPtr[x];
                    instance->caret = (uint)x;
                }
            }
            instance->caret = instance->caret + 1;
            oPtr[instance->capacity + 1] = (byte)'\0';

            return instance;
        }

        #region TESTS
        internal static void __RunTests()
        {
            __Test_Append();
            __Test_LengthAndCapacity();
            __Test_RemoveAt();
        }

        private static void __Test_Append()
        {
            StringBuilder* sb = StringBuilder.CREATE(5);

            sb->AppendChar((byte)'a');
            Diagnostics.Assert(sb->buffer->Pointer[0] == (byte)'a', "StringBuilder.AppendChar(byte): AppendChar not working correctly!");
            sb->Append("b");
            Diagnostics.Assert(sb->buffer->Pointer[1] == (byte)'b', "StringBuilder.Append(string): Append not working correctly!");

            ADC.MemoryManager.Free((void*)sb);
        }

        private static void __Test_LengthAndCapacity()
        {
            StringBuilder* sb = StringBuilder.CREATE(50);
            sb->Append("abcd");
            sb->Append("e");

            Diagnostics.Assert(sb->Length == 5, "StringBuilder.Append(string): Length is being mismanaged!");
            Diagnostics.Assert(sb->capacity == 50, "StringBuilder.Append(string): Capacity is being mismanaged!");

            sb->Clear();
            Diagnostics.Assert(sb->Length == 0, "StringBuilder.Clear(): Not clearing correctly");

            ADC.MemoryManager.Free((void*)sb);
        }

        private static void __Test_RemoveAt()
        {
            StringBuilder* sb = StringBuilder.CREATE(25);
            sb->Append("abcd");

            sb->RemoveAt(1, 2);
            Diagnostics.Assert(sb->buffer->Compare("ad") == 0, "StringBuilder.RemoveAt(int,int): Middle-of-string removal is not working!");

            sb->Append("e");
            sb->RemoveAt(0, 2);
            Diagnostics.Assert(sb->buffer->Compare("e") == 0, "StringBuilder.RemoveAt(int,int): Beginning-of-string removal is not working!");

            sb->Append("fg");
            sb->RemoveAt(1, 2);
            Diagnostics.Assert(sb->buffer->Compare("e") == 0, "StringBuilder.RemoveAt(int,int): End-of-string removal is not working!");

            ADC.MemoryManager.Free((void*)sb);
        }

        #endregion
    }
}
