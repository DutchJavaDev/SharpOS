
using System;

namespace SharpOS {

	public class ASCII {
		public static byte ToLower (byte ch)
		{
			if (ch >= (byte)'A' && ch <= (byte)'Z')
				return (byte)(ch - ((byte)'A' - (byte)'a'));
			else
				return ch;
		}
		
		public static byte ToUpper (byte ch)
		{
			if (ch >= (byte)'a' && ch <= (byte)'z')
				return (byte) (ch - ((byte)'a' - (byte)'A'));
			else
				return ch;
		}
		
		public static bool IsLowerAlpha (byte ch)
		{
			if (ch >= (byte)'a' && ch <= (byte)'z')
				return true;
			else
				return false;
		}
		
		public static bool IsUpperAlpha (byte ch)
		{
			if (ch >= (byte)'A' && ch <= (byte)'Z')
				return true;
			else
				return false;
		}
		
		public static bool IsAlpha (byte ch)
		{
			return IsLowerAlpha (ch) || IsUpperAlpha (ch);
		}
		
		public static bool IsBackspace (byte ch)
		{
			if (ch == 26)
				return true;
			
			return false;
		}
		
		public static bool IsNumeric (byte ch)
		{
			if (ch >= (byte)'0' && ch <= (byte)'9')
				return true;
			else
				return false;
		}
	}
}