using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace BSharp
{
	class Program
	{
		public enum bfOpCodes { 
			INCMTP = '>', 
			DECMTP = '<', 
			INC = '+', 
			DEC = '-', 
			OUTPUT = '.', 
			INPUT = ',', 
			JMPFWD = '[', 
			JMPBWD = ']'
		};										

		enum bfComplexOpCodes { 
			INCMTP = '>', 
			DECMTP = '<', 
			INC = '+', 
			DEC = '-', 
			OUTPUT = '.', 
			INPUT = ',', 
			JMPFWD = '[', 
			JMPBWD = ']',
			CLR = '0',
			ADD = '#',
			SUB = '~'
		};

		struct ComplexOp{
			public ComplexOp(bfComplexOpCodes op, params int[] args) 
			{ 
				Parameters = new List<int>();
				OpCode = op;
				foreach(int arg in args) { Parameters.Add(arg); }
			}
			public bfComplexOpCodes OpCode;
			public List<int> Parameters;
		}
			
		static readonly bfOpCodes[] AdditionLoop = new bfOpCodes[] {
			bfOpCodes.JMPFWD,
			bfOpCodes.DEC,
			bfOpCodes.INCMTP,
			bfOpCodes.INC,
			bfOpCodes.DECMTP,
			bfOpCodes.JMPBWD
		};

		static readonly bfOpCodes[] SubtractionLoop = new bfOpCodes[] {
			bfOpCodes.JMPFWD,
			bfOpCodes.DEC,
			bfOpCodes.DECMTP,
			bfOpCodes.DEC,
			bfOpCodes.INCMTP,
			bfOpCodes.JMPBWD
		};

		static int commandIndex = 0;
		static int memoryIndex = 0;
		const int bfRange = 30000;
		static byte[] bfProgramMemory = new byte[bfRange];
		static bfOpCodes[] bfCommandMemory = new bfOpCodes[bfRange];
		static List<ComplexOp> bfComplexMemory = new List<ComplexOp> ();

		static void Main(string[] args)
		{
			//Check parameters
			if(args.Length > 1)
			{
			    Console.WriteLine("BrainSharp Interpreter   -   Invalid number of arguments");
			    return;
			}
			else if(args.Length == 1)
			{
				if(!File.Exists(args[0]))
				{
				    Console.WriteLine("BrainSharp Interpreter   -   File Not Found");
				    return;
				}

				string bfScript = System.IO.File.ReadAllText("test.bf", Encoding.ASCII);
				commandIndex = 0;
				foreach(char instruction in bfScript)
				{
					if (Enum.IsDefined(typeof(bfOpCodes), (int)instruction))
						bfCommandMemory[commandIndex++] = (bfOpCodes)instruction;
				}
			}
				
			//Optimization
			for(int i = 0; i < commandIndex; i++) 
			{
				int argA = 0;
				int argB = 0;
				int argC = 0;
				int skipLen = 0;

				switch(bfCommandMemory[i]) {
				case bfOpCodes.INCMTP:
				case bfOpCodes.DECMTP:
				case bfOpCodes.INC:
				case bfOpCodes.DEC:
					bfComplexMemory.Add (new ComplexOp ((bfComplexOpCodes)bfCommandMemory [i], CountSequence (i, out i)));
					break;
				case bfOpCodes.JMPFWD:
					if (isClearLoop (i)) 
					{
						bfComplexMemory.Add (new ComplexOp (bfComplexOpCodes.CLR));
						i = i + 2;
					}
					else if(isAdditionLoop(i, out argA, out argB, out argC, out skipLen))
					{
						bfComplexMemory.Add (new ComplexOp (bfComplexOpCodes.ADD, argA, argB, argC));	
						i = i + skipLen - 1;
					}
					else if(isSubtractionLoop (i, out argA, out argB, out skipLen)) {
						bfComplexMemory.Add (new ComplexOp (bfComplexOpCodes.SUB, argA, argB));	
						i = i + skipLen - 1;
					}
					else
						bfComplexMemory.Add (new ComplexOp ((bfComplexOpCodes)bfCommandMemory [i]));					
					break;				
				default:
					bfComplexMemory.Add (new ComplexOp ((bfComplexOpCodes)bfCommandMemory [i]));
					break;
				}
			}

			//Start Executing optimized code
			System.Diagnostics.Stopwatch executionTime = System.Diagnostics.Stopwatch.StartNew();
			commandIndex = 0;
			while(commandIndex < bfComplexMemory.Count)
			{
				ComplexOp op = bfComplexMemory [(int)commandIndex];
				switch(op.OpCode)
				{
				case bfComplexOpCodes.INCMTP:
					memoryIndex += op.Parameters[0];
					if (memoryIndex >= bfRange)
						memoryIndex = 0;
					break;
				case bfComplexOpCodes.DECMTP:
					memoryIndex -= op.Parameters[0];
					if (memoryIndex < 0)
						memoryIndex = bfRange-1;
					break;
				case bfComplexOpCodes.INC:
					bfProgramMemory[memoryIndex] += (byte)op.Parameters[0];
					break;
				case bfComplexOpCodes.DEC:
					bfProgramMemory[memoryIndex] -= (byte)op.Parameters[0];
					break;
				case bfComplexOpCodes.OUTPUT:
					Console.Write(Convert.ToChar(bfProgramMemory[memoryIndex]));
					break;
				case bfComplexOpCodes.INPUT:
					//Get Input
					break;
				case bfComplexOpCodes.JMPFWD:
					if (bfProgramMemory[memoryIndex] == 0)
						JumpOp(true);
					break;
				case bfComplexOpCodes.JMPBWD:
					if (bfProgramMemory[memoryIndex] != 0)
						JumpOp(false);
					break;
				case bfComplexOpCodes.CLR:
					bfProgramMemory [memoryIndex] = 0;
					break;
				case bfComplexOpCodes.ADD:					
					bfProgramMemory [memoryIndex + op.Parameters[0]] += (byte)((bfProgramMemory [memoryIndex + op.Parameters[0] - op.Parameters[1]]) * op.Parameters[2]);
					bfProgramMemory [memoryIndex + op.Parameters[0] - op.Parameters[1]] = 0;
					break;
				case bfComplexOpCodes.SUB:
					bfProgramMemory [memoryIndex - op.Parameters[0]] -= (byte)((bfProgramMemory [memoryIndex - op.Parameters[0] + op.Parameters[1]]));
					bfProgramMemory [memoryIndex - op.Parameters[0] + op.Parameters[1]] = 0;
					break;
				default:                            
					break;
				}
				commandIndex++;
			}
			executionTime.Stop ();
			Console.WriteLine ("Execution took: " + executionTime.ElapsedMilliseconds/1000 + " seconds");
			Console.ReadLine ();
		}
			
		public static void JumpOp(bool isForward)
		{
			int currentPosition = commandIndex;
			int loop = 1;
			int signval = isForward ? 1 : -1;

			while(loop > 0)
			{
				currentPosition = currentPosition + signval;
				if (bfComplexMemory [currentPosition].OpCode == (bfComplexOpCodes)bfOpCodes.JMPFWD)
					loop = loop + signval;
				else if (bfComplexMemory [currentPosition].OpCode == (bfComplexOpCodes)bfOpCodes.JMPBWD)
					loop = loop + (-1 * signval);
			}
			commandIndex = isForward ? currentPosition : currentPosition - 1;
		}

		public static int CountSequence(int startPosition, out int newIndex)
		{
			int count = 0;
			bfOpCodes currentOp = bfCommandMemory [startPosition];
			while (bfCommandMemory [startPosition] == currentOp) 
			{
				startPosition++;
				count++;
			}
			newIndex = startPosition - 1;
			return count;
		}

		public static bool isClearLoop(int startPosition)
		{
			if (bfCommandMemory [startPosition] == bfOpCodes.JMPFWD
			   && (bfCommandMemory [startPosition + 1] == bfOpCodes.INC 
					|| bfCommandMemory [startPosition + 1] == bfOpCodes.DEC)
				&& bfCommandMemory [startPosition + 2] == bfOpCodes.JMPBWD) 
			{
				return true;
			}
			return false;
		}	

		public static bool isAdditionLoop(int startPosition, out int posA, out int posB, out int multi, out int skip)
		{
			bool returnValue = true;
			posA = posB = multi = 0;
			foreach (bfOpCodes addOp in AdditionLoop) {
				if (addOp != bfCommandMemory [startPosition]) {
					returnValue = false;
					break;
				}

				if (addOp == bfOpCodes.INCMTP)
					posA = CountSequence (startPosition, out startPosition);
				else if (addOp == bfOpCodes.DECMTP)
					posB = CountSequence (startPosition, out startPosition);
				else if (addOp == bfOpCodes.INC)
					multi = CountSequence (startPosition, out startPosition);
				startPosition++;
			}

			skip = (AdditionLoop.Length - 2) + posA + posB;
			return returnValue;
		}

		public static bool isSubtractionLoop(int startPosition, out int posA, out int posB, out int skip)
		{
			bool returnValue = true;
			posA = posB = 0;
			foreach (bfOpCodes addOp in SubtractionLoop) {
				if (addOp != bfCommandMemory [startPosition]) {
					returnValue = false;
					break;
				}

				if (addOp == bfOpCodes.INCMTP)
					posA = CountSequence (startPosition, out startPosition);
				else if (addOp == bfOpCodes.DECMTP)
					posB = CountSequence (startPosition, out startPosition);
				startPosition++;
			}

			skip = (SubtractionLoop.Length - 2) + posA + posB;
			return returnValue;
		}
	}
}
