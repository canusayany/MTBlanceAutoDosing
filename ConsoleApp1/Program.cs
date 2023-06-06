using ClassLibrary1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MTXPR205 mTXPR205 = new MTXPR205();
            mTXPR205.UpdateInfo();
            mTXPR205.Connect();
            mTXPR205.SetDoorPosition(220, 0);
            Console.ReadLine();
        }
    }
}
