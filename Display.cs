namespace Chip8;

internal class Display
{
    public const int DisplayWidth = 64;
    public const int DisplayHeight = 32;

    public byte[] Buffer = new byte[DisplayWidth * DisplayHeight];

    public bool Draw(int x, int y, byte[] chunk)
    {
        var collision = false;

        for (int blockOffset = 0; blockOffset < chunk.Length; blockOffset++)
        {
            var block = chunk[blockOffset];
            var positionY = (y + blockOffset) % DisplayHeight;

            for (int pixelOffset = 0; pixelOffset < 8; pixelOffset++)
            {
                // Get buffer index from x and y coordinates.
                var positionX = (x + pixelOffset) % DisplayWidth;
                var index = FromCoordinates(positionX, positionY);

                var bufferPixel = Buffer[index];
                var spritePixel = (block >> (7 - pixelOffset)) & 1;

                var pixel = (byte)(bufferPixel ^ spritePixel);

                Buffer[index] = pixel;

                if (spritePixel == 1 && pixel == 0)
                {
                    collision = true;
                }
            }
        }

        return collision;
    }

    public void Clear()
    {
        Buffer = new byte[DisplayWidth * DisplayHeight];
    }

    private static int FromCoordinates(int x, int y)
    {
        return x + DisplayWidth * y;
    }
}
