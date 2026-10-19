namespace Chip8;

using Raylib_cs;

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
        var chip8Time = 0.0;

        Raylib.InitWindow(640, 320, "Chip8 Emulator");

        while (!Raylib.WindowShouldClose())
        {
            // Update emulator's state based on Raylib's delta time.
            float dt = Raylib.GetFrameTime();

            chip8.PerformCpuCycle(dt);
            chip8Time += dt;

            if (chip8Time > 1 / 60)
            {
                chip8.DecrementTimers();
                chip8Time -= 1 / 60;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            foreach (int x in Enumerable.Range(0, Display.DisplayWidth))
            {
                foreach (int y in Enumerable.Range(0, Display.DisplayHeight))
                {
                    // Calculate the pixel's position in the display buffer and determine
                    // whether it should be drawn.
                    int index = x + Display.DisplayWidth * y;

                    if (chip8.Display.Buffer[index] == 1)
                    {
                        Raylib.DrawRectangle(x * 10, y * 10, 10, 10, Color.White);
                    }
                }
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}