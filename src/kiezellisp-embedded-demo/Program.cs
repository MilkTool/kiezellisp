﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kiezellisp_embedded_demo
{
    class Program
    {
        static void Main( string[] args )
        {
            try
            {
                Kiezel.EmbeddedMode.Init( consoleMode: true, debugMode: false );
                var a = Kiezel.EmbeddedMode.Funcall( "+", 2, 3 );
                Kiezel.EmbeddedMode.Funcall( "print-line", a );
                var b = Kiezel.EmbeddedMode.Funcall( "+", a, a );
                Kiezel.EmbeddedMode.Funcall( "++", 3, 4 );
                
            }
            catch ( Exception ex )
            {
                var s = Kiezel.EmbeddedMode.GetDiagnostics( ex );
                Console.WriteLine( s );
            }
        }
    }
}