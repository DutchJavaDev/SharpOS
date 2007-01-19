/**
 *  (C) 2006-2007 Mircea-Cristian Racasan <darx_kies@gmx.net>
 * 
 *  Licensed under the terms of the GNU GPL License version 2.
 * 
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using SharpOS;
using SharpOS.AOT.IR;
using SharpOS.AOT.X86;


namespace SharpOS
{
    public class AOTOS
    {
        static void Main(string[] args)
        {
            //try
            {
                Engine engine = new Engine();
                engine.Run(new Assembly(), "SharpOS.dll", "SharpOS.bin");

                SharpOS.ClearScreen(1, 2);

                /*Tests.TypeD2L(10.0d);
                Tests.TypeBool(true);
                Tests.TstDouble(2.3d, 5.3d);
                Tests.TstFloat(2.3f, 5.3f);
                Tests.Types(1, 2, 3, 4, 5, 6, 7, 8);
                Tests.TypesU(1, 2, 3, 4, 5);*/
            }
            //catch (Exception ex)
            {
                //Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
