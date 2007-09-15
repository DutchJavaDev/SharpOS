//
// System.Threading.WaitHandle.cs
//
// Author:
// 	Dick Porter (dick@ximian.com)
// 	Gonzalo Paniagua Javier (gonzalo@ximian.com
//
// (C) 2002,2003 Ximian, Inc.	(http://www.ximian.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Permissions;

#if NET_2_0
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif

namespace System.Threading
{
	public abstract class WaitHandle : MarshalByRefObject, IDisposable
	{
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private static extern bool WaitAll_internal(WaitHandle[] handles, int ms, bool exitContext);
		
		static void CheckArray (WaitHandle [] handles, bool waitAll)
		{
			if (handles == null)
				throw new ArgumentNullException ("waitHandles");

			int length = handles.Length;
			if (length > 64)
				throw new NotSupportedException ("Too many handles");

#if false
			//
			// Although we should thrown an exception if this is an STA thread,
			// Mono does not know anything about STA threads, and just makes
			// things like Paint.NET not even possible to work.
			//
			// See bug #78455 for the bug this is supposed to fix. 
			// 
			if (waitAll && length > 1 && IsSTAThread)
				throw new NotSupportedException ("WaitAll for multiple handles is not allowed on an STA thread.");
#endif
			foreach (WaitHandle w in handles) {
				if (w == null)
					throw new ArgumentNullException ("waitHandles", "null handle");

#if NET_2_0
				if (w.safe_wait_handle == null)
					throw new ArgumentException ("null element found", "waitHandle");
#else
				if (w.os_handle == InvalidHandle)
					throw new ArgumentException ("null element found", "waitHandle");
#endif
			}
		}

		static bool IsSTAThread {
			get {
				bool isSTA = Thread.CurrentThread.ApartmentState ==
					ApartmentState.STA;

				// FIXME: remove this check after Thread.ApartmentState
				// has been properly implemented.
				if (!isSTA) {
					Assembly asm = Assembly.GetEntryAssembly ();
					if (asm != null)
						isSTA = asm.EntryPoint.GetCustomAttributes (typeof (STAThreadAttribute), false).Length > 0;
				}

				return isSTA;
			}
		}
		
		public static bool WaitAll(WaitHandle[] waitHandles)
		{
			CheckArray (waitHandles, true);
			return(WaitAll_internal(waitHandles, Timeout.Infinite, false));
		}

		public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
		{
			CheckArray (waitHandles, true);
			try {
				if (exitContext) SynchronizationAttribute.ExitContext ();
				return(WaitAll_internal(waitHandles, millisecondsTimeout, false));
			}
			finally {
				if (exitContext) SynchronizationAttribute.EnterContext ();
			}
		}

		public static bool WaitAll(WaitHandle[] waitHandles,
					   TimeSpan timeout,
					   bool exitContext)
		{
			CheckArray (waitHandles, true);
			long ms = (long) timeout.TotalMilliseconds;
			
			if (ms < -1 || ms > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("timeout");

			try {
				if (exitContext) SynchronizationAttribute.ExitContext ();
				return (WaitAll_internal (waitHandles, (int) ms, exitContext));
			}
			finally {
				if (exitContext) SynchronizationAttribute.EnterContext ();
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private static extern int WaitAny_internal(WaitHandle[] handles, int ms, bool exitContext);

		// LAMESPEC: Doesn't specify how to signal failures
		public static int WaitAny(WaitHandle[] waitHandles)
		{
			CheckArray (waitHandles, false);
			return(WaitAny_internal(waitHandles, Timeout.Infinite, false));
		}

		public static int WaitAny(WaitHandle[] waitHandles,
					  int millisecondsTimeout,
					  bool exitContext)
		{
			CheckArray (waitHandles, false);
			try {
				if (exitContext) SynchronizationAttribute.ExitContext ();
				return(WaitAny_internal(waitHandles, millisecondsTimeout, exitContext));
			}
			finally {
				if (exitContext) SynchronizationAttribute.EnterContext ();
			}
		}

		public static int WaitAny(WaitHandle[] waitHandles,
					  TimeSpan timeout, bool exitContext)
		{
			CheckArray (waitHandles, false);
			long ms = (long) timeout.TotalMilliseconds;
			
			if (ms < -1 || ms > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("timeout");

			try {
				if (exitContext) SynchronizationAttribute.ExitContext ();
				return (WaitAny_internal(waitHandles, (int) ms, exitContext));
			}
			finally {
				if (exitContext) SynchronizationAttribute.EnterContext ();
			}
		}

		[MonoTODO]
		public WaitHandle() {
			// FIXME
		}

		public virtual void Close() {
			Dispose(true);
			GC.SuppressFinalize (this);
		}

		public const int WaitTimeout = 258;

#if NET_2_0
		//
		// In 2.0 we use SafeWaitHandles instead of IntPtrs
		//
		SafeWaitHandle safe_wait_handle;

		[Obsolete ("In the profiles > 2.x, use SafeHandle instead of Handle")]
		public virtual IntPtr Handle {
			get {
				return safe_wait_handle.DangerousGetHandle ();
			}

			[SecurityPermission (SecurityAction.LinkDemand, UnmanagedCode = true)]
			[SecurityPermission (SecurityAction.InheritanceDemand, UnmanagedCode = true)]
			set {
				//
				// Notice, from the 2.x documentation:
				//    Assigning a new value to the Handle property, will not release
				//    the previous handle, this could lead to a leak
				//
				safe_wait_handle = new SafeWaitHandle (value, false);
			}
		}
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private extern bool WaitOne_internal(IntPtr handle, int ms, bool exitContext);

		protected virtual void Dispose (bool explicitDisposing)
		{
			if (!disposed){
				disposed = true;

				//
				// This is only the case if the handle was never properly initialized
				// most likely a but in the derived class
				//
				if (safe_wait_handle == null)
					return;

				lock (this){
					if (safe_wait_handle != null)
						safe_wait_handle.Dispose ();
				}
			}
		}

		public SafeWaitHandle SafeWaitHandle {
			get {
				return safe_wait_handle;
			}

			set {
				if (safe_wait_handle != null)
					safe_wait_handle.Close ();
				
				safe_wait_handle = value;
			}
		}

		public virtual bool WaitOne()
		{
			CheckDisposed ();
			bool release = false;
			try {
				safe_wait_handle.DangerousAddRef (ref release);
				return (WaitOne_internal(safe_wait_handle.DangerousGetHandle (), Timeout.Infinite, false));
			} finally {
				if (release)
					safe_wait_handle.DangerousRelease ();
			}
		}

		public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
		{
			CheckDisposed ();
			bool release = false;
			try {
				if (exitContext)
					SynchronizationAttribute.ExitContext ();
				safe_wait_handle.DangerousAddRef (ref release);
				return (WaitOne_internal(safe_wait_handle.DangerousGetHandle (), millisecondsTimeout, exitContext));
			} finally {
				if (exitContext)
					SynchronizationAttribute.EnterContext ();
				if (release)
					safe_wait_handle.DangerousRelease ();
			}
		}

		public virtual bool WaitOne(TimeSpan timeout, bool exitContext)
		{
			CheckDisposed ();
			long ms = (long) timeout.TotalMilliseconds;
			if (ms < -1 || ms > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("timeout");

			bool release = false;
			try {
				if (exitContext)
					SynchronizationAttribute.ExitContext ();
				safe_wait_handle.DangerousAddRef (ref release);
				return (WaitOne_internal(safe_wait_handle.DangerousGetHandle (), (int) ms, exitContext));
			}
			finally {
				if (exitContext)
					SynchronizationAttribute.EnterContext ();
				if (release)
					safe_wait_handle.DangerousRelease ();
			}
		}

		internal void CheckDisposed ()
		{
			if (disposed || safe_wait_handle == null)
				throw new ObjectDisposedException (GetType ().FullName);
		}
#else
		private IntPtr os_handle = InvalidHandle;
		
		public virtual IntPtr Handle {
			get {
				return(os_handle);
			}
				
			[SecurityPermission (SecurityAction.LinkDemand, UnmanagedCode = true)]
			[SecurityPermission (SecurityAction.InheritanceDemand, UnmanagedCode = true)]
			set {
				os_handle=value;
			}
		}

		internal void CheckDisposed ()
		{
			if (disposed || os_handle == InvalidHandle)
				throw new ObjectDisposedException (GetType ().FullName);
		}
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private extern bool WaitOne_internal(IntPtr handle, int ms, bool exitContext);

		protected virtual void Dispose(bool explicitDisposing) {
			// Check to see if Dispose has already been called.
			if (!disposed) {
				disposed=true;
				if (os_handle == InvalidHandle)
					return;

				lock (this) {
					if (os_handle != InvalidHandle) {
						NativeEventCalls.CloseEvent_internal (os_handle);
						os_handle = InvalidHandle;
					}
				}
			}
		}
		
		public virtual bool WaitOne()
		{
			CheckDisposed ();
			return(WaitOne_internal(os_handle, Timeout.Infinite, false));
		}

		public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
		{
			CheckDisposed ();
			try {
				if (exitContext) SynchronizationAttribute.ExitContext ();
				return(WaitOne_internal(os_handle, millisecondsTimeout, exitContext));
			}
			finally {
				if (exitContext) SynchronizationAttribute.EnterContext ();
			}
		}

		public virtual bool WaitOne(TimeSpan timeout, bool exitContext)
		{
			CheckDisposed ();
			long ms = (long) timeout.TotalMilliseconds;
			if (ms < -1 || ms > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("timeout");

			try {
				if (exitContext) SynchronizationAttribute.ExitContext ();
				return (WaitOne_internal(os_handle, (int) ms, exitContext));
			}
			finally {
				if (exitContext) SynchronizationAttribute.EnterContext ();
			}
		}
#endif

		protected static readonly IntPtr InvalidHandle = IntPtr.Zero;
		bool disposed = false;

		void IDisposable.Dispose() {
			Dispose(true);
			// Take yourself off the Finalization queue
			GC.SuppressFinalize(this);
		}
		
		~WaitHandle() {
			Dispose(false);
		}
	}
}