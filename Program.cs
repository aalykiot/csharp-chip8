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
        var chip8dt = 0.0;

        Raylib.InitWindow(640, 320, "Chip8 Emulator");

        while (!Raylib.WindowShouldClose())
        {
            // Update emulator's state based on Raylib's delta time.
            float dt = Raylib.GetFrameTime();

            chip8.PerformCpuCycle(dt);
            chip8dt += dt;

            if (chip8dt > 1.0 / 60.0)
            {
                chip8.DecrementTimers();
                chip8dt -= 1.0 / 60.0;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            for (int x = 0; x < Display.DisplayWidth; x++)
            {
                for (int y = 0; y < Display.DisplayHeight; y++)
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
