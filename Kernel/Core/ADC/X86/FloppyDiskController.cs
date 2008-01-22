// 
// (C) 2006-2007 The SharpOS Project Team (http://www.sharpos.org)
//
// Authors:
//	Jae Hyun Roh <wonbear@gmail.com>
//
// Licensed under the terms of the GNU GPL v3,
//  with Classpath Linking Exception for Libraries
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using SharpOS.Kernel;
using SharpOS.AOT.X86;
using SharpOS.AOT.IR;
using ADC = SharpOS.Kernel.ADC;

namespace SharpOS.Kernel.ADC.X86
{
	public unsafe class FloppyDiskController
	{
		#region Constants
		
		const string	FLOPPYDISKCONTROLLER_HANDLER = "FLOPPYDISKCONTROLLER_HANDLER";

		const int BYTES_PER_SECTOR = 512;
		const int SECTORS_PER_TRACK = 18;
		const int BYTES_PER_TRACK = BYTES_PER_SECTOR * SECTORS_PER_TRACK;
		
		#endregion
		#region Enumerations
		private enum FIFOCommand
		{
			ReadTrack			= 0x02,
			Specify				= 0x03,
			SenseDriveStatus 	= 0x04,
			WriteData			= 0x05,
			ReadData			= 0x06,
			Recalibrate			= 0x07,
			SenseInterrupt		= 0x08,
			WriteDeletedData	= 0x09,
			ReadID				= 0x0A,

			ReadDeletedData		= 0x0C,
			FormatTrack			= 0x0D,

			Seek				= 0x0F,
			Version				= 0x10,
			ScanEqual			= 0x11,
			PerpendicularMode	= 0x12,
			Configure			= 0x13,

			Verify				= 0x16,

			ScanLowOrEqual		= 0x19,

			ScanHighOrEqual		= 0x1D,

			Write				= 0xC5,
			Read				= 0xE6,
			Format				= 0x4D
		};

		[Flags]
		private enum DORFlags : byte
		{
			MotorEnableShift	= 0x04,
			MotorEnableMask		= 0x0F,

			EnableDMA			= 0x08,
			EnableController	= 0x04,
			EnableAllMotors		= 0x10,
			DisableAll			= 0x00,

			DriveSelectShift	= 0x00,
			DriveSelectMask		= 0x03,
		};
		#endregion

		static bool fddInterrupt = false;
		public static void Setup ()
		{
			IDT.RegisterIRQ (IDT.Interrupt.FloppyDiskController, Stubs.GetFunctionPointer (FLOPPYDISKCONTROLLER_HANDLER));
			TurnOffMotor ();
		}

		[SharpOS.AOT.Attributes.Label (FLOPPYDISKCONTROLLER_HANDLER)]
		static unsafe void FloppyDiskControllerHandler (IDT.ISRData data)
		{
			SetInterruptOccurred (true);
			
			// This is not necesarry, already done in code wrapped around this:
			//IO.Out8(IO.Port.Master_PIC_CommandPort, 0x20);
		}
		
		public static bool WaitForInterrupt()
		{
			int counter = 0;
			while (HasInterruptOccurred () == false && counter < 10000)
				counter++;
			return (counter < 10000);
		}

		public static bool HasInterruptOccurred ()
		{
			return fddInterrupt;
		}

		public static void SetInterruptOccurred (bool interruptOccurred)
		{
			fddInterrupt = interruptOccurred;
		}

		public static void TurnOffMotor ()
		{
			IO.WriteByte (IO.Port.FDC_DORPort,
				(byte) DORFlags.DisableAll);
		}

		public static void TurnOnMotor ()
		{
			IO.WriteByte (IO.Port.FDC_DORPort,
						(byte)(DORFlags.EnableDMA | DORFlags.EnableController | DORFlags.EnableAllMotors));
		}

		private unsafe static void SendCommandToFDC (byte command)
		{
			Barrier.Enter();
			byte status = 0;

			do {
				status = IO.ReadByte (IO.Port.FDC_StatusPort);
			}
			while ((status & 0xC0) != 0x80); //TODO: implement timeout

			IO.WriteByte (IO.Port.FDC_DataPort, command);
			Barrier.Exit();
		}

		private unsafe static void SendDataToFDC (byte data)
		{
			Barrier.Enter();
			byte status = 0;

			do {
				status = IO.ReadByte (IO.Port.FDC_StatusPort);
			}
			while ((status & 0xC0) != 0x80); //TODO: implement timeout

			IO.WriteByte (IO.Port.FDC_DataPort, data);
			Barrier.Exit();
		}

		internal static byte* diskBuffer = null;

		public unsafe static void Read(byte* buffer, uint offset, uint length)
		{
			if (diskBuffer == null)
			{
				// TODO: allocate a piece of memory in a memory location that can be used by DMA
				diskBuffer = (byte*) MemoryManager.Allocate ((uint)BYTES_PER_TRACK);
			}

			uint readBlockCount = (length + (BYTES_PER_TRACK - 1)) / BYTES_PER_TRACK;

			for (uint b = 0; b < readBlockCount; ++b)
			{
				byte sector = (byte)(offset / BYTES_PER_SECTOR);

				byte head = (byte)((sector % (SECTORS_PER_TRACK * 2)) / SECTORS_PER_TRACK);
				byte track = (byte)(sector / (SECTORS_PER_TRACK * 2));

				sector = (byte)((sector % SECTORS_PER_TRACK) + 1);

				MemoryUtil.MemSet(0, (uint)(diskBuffer), (uint)BYTES_PER_TRACK);
				ReadData(head, track, sector);
				
				MemoryUtil.MemCopy((uint)(diskBuffer), (uint)(buffer + offset), (uint)BYTES_PER_TRACK);
				//for (uint index = offset, index2 = 0; index < offset + BYTES_PER_TRACK; ++index, ++index2)
				//	buffer[index] = diskBuffer[index2];

				offset += BYTES_PER_TRACK;
			}
		}

		//TODO: replace integer values with enums or describe in comments
		public unsafe static void ReadData(byte head, byte track, byte sector)
		{
			// ...this section should be skipped if motor is already running
			Barrier.Enter();
			{
				SetInterruptOccurred (false);
				TurnOffMotor ();
				TurnOnMotor ();
			}
			Barrier.Exit();

			if (!WaitForInterrupt())
				return;
					
			// ...do we need to recalibrate every time?
			Barrier.Enter();
			{
				SetInterruptOccurred (false);
				SendCommandToFDC((byte)FIFOCommand.Recalibrate);
				SendDataToFDC (0x00);
			}
			Barrier.Exit();

			if (!WaitForInterrupt())
				return;
						
			Barrier.Enter();
			{
				SetInterruptOccurred (false);
				SendCommandToFDC((byte)FIFOCommand.Seek);
				SendCommandToFDC((byte)((head << 2) + 0));
				SendCommandToFDC(track);
			}
			Barrier.Exit();

			if (!WaitForInterrupt())
				return;

			DMA.SetupChannel(DMAChannel.Channel2,
				(byte)(((uint)diskBuffer >> 16) & 0xff),
				(ushort)(((uint)diskBuffer & 0xffff)),
				BYTES_PER_TRACK,
				DMAMode.Read);
				
			Barrier.Enter();
			{
				SetInterruptOccurred (false);
				
				SendCommandToFDC((byte)FIFOCommand.Read);
				SendDataToFDC ((byte)((head << 2) + 0));
				SendDataToFDC (track);
				SendDataToFDC (head);
				SendDataToFDC (sector);
				SendDataToFDC (2);		// 512 bytes per sector
				SendDataToFDC (SECTORS_PER_TRACK);
				SendDataToFDC (27);
				SendDataToFDC (0xff);	// DTL (bytes to transfer) = unused
			}
			Barrier.Exit();

			if (!WaitForInterrupt())
				return;
						
			Barrier.Enter();
			{
				TurnOffMotor();
			}
			Barrier.Exit();
		}
	}
}
