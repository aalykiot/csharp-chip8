namespace Chip8;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            throw new Exception("A Chip8 ROM must be provided.");
        }

        var program = File.ReadAllBytes(args[0]);
        var chip8 = new Chip8Emulator(program);

        chip8.PerformCpuCycle(0.05);
    }
}