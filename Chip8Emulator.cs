namespace Chip8;

abstract record Instruction;

record ClearDisplay : Instruction;
record Return : Instruction;
record Jump(ushort Address) : Instruction;
record Call(ushort Address) : Instruction;
record SkipIfEqualsByte(byte Register, byte Value) : Instruction;
record SkipIfNotEqualsByte(byte Register, byte Value) : Instruction;
record SkipIfEqual(byte First, byte Second) : Instruction;
record LoadByte(byte Destination, byte Value) : Instruction;
record AddByte(byte Register, byte Value) : Instruction;
record Move(byte Destination, byte Source) : Instruction;
record Or(byte Destination, byte Source) : Instruction;
record And(byte Destination, byte Source) : Instruction;
record Xor(byte Destination, byte Source) : Instruction;
record Add(byte Destination, byte Source) : Instruction;
record Sub(byte Destination, byte Source) : Instruction;
record ShiftRight(byte Register) : Instruction;
record ReverseSub(byte Destination, byte Source) : Instruction;
record ShiftLeft(byte Register) : Instruction;
record SkipIfNotEqual(byte First, byte Second) : Instruction;
record LoadI(ushort Address) : Instruction;
record JumpPlusZero(ushort Address) : Instruction;
record Random(byte Register, byte Mask) : Instruction;
record Draw(byte RegisterX, byte RegisterY, byte Height) : Instruction;
record SkipIfPressed(byte Register) : Instruction;
record SkipIfNotPressed(byte Register) : Instruction;
record LoadDelayTimer(byte Register) : Instruction;
record WaitForKeyPress(byte Register) : Instruction;
record SetDelayTimer(byte Register) : Instruction;
record SetSoundTimer(byte Register) : Instruction;
record AddToI(byte Register) : Instruction;
record LoadSprite(byte Register) : Instruction;
record BCDRepresentation(byte Register) : Instruction;
record StoreRegisters(byte Register) : Instruction;
record LoadRegisters(byte Register) : Instruction;

abstract record ProgramCounterState;

record Next : ProgramCounterState;
record Skip : ProgramCounterState;
record JumpTo(ushort Address) : ProgramCounterState;

class Chip8Emulator
{
    private const int CpuClock = 600;
    private const int ProgramMemoryOffset = 0x200;

    private static readonly byte[] FontMap = [
        0xF0, 0x90, 0x90, 0x90, 0xF0,
        0x20, 0x60, 0x20, 0x20, 0x70,
        0xF0, 0x10, 0xF0, 0x80, 0xF0,
        0xF0, 0x10, 0xF0, 0x10, 0xF0,
        0x90, 0x90, 0xF0, 0x10, 0x10,
        0xF0, 0x80, 0xF0, 0x10, 0xF0,
        0xF0, 0x80, 0xF0, 0x90, 0xF0,
        0xF0, 0x10, 0x20, 0x40, 0x40,
        0xF0, 0x90, 0xF0, 0x90, 0xF0,
        0xF0, 0x90, 0xF0, 0x10, 0xF0,
        0xF0, 0x90, 0xF0, 0x90, 0x90,
        0xE0, 0x90, 0xE0, 0x90, 0xE0,
        0xF0, 0x80, 0x80, 0x80, 0xF0,
        0xE0, 0x90, 0x90, 0x90, 0xE0,
        0xF0, 0x80, 0xF0, 0x80, 0xF0,
        0xF0, 0x80, 0xF0, 0x80, 0x80,
  ];

    private static readonly System.Random _random = new();
    public readonly Display Display = new();

    private ushort _programCounter = ProgramMemoryOffset;
    private ushort _iRegister = 0;

    private readonly ushort[] _stack = new ushort[16];

    private readonly byte[] _registers = new byte[16];
    private readonly byte[] _memory = new byte[4096];
    private readonly bool[] _keyboard = new bool[16];

    private byte _stackPointer = 0;
    private byte _delayRegister = 0;
    private byte _soundRegister = 0;
    private byte? _keyboardWaitKey = null;


    public Chip8Emulator(byte[] program)
    {
        // Load built-in fonts and the program into the emulator's memory (this 
        // can fail for some many reason but I'll ignore it for now).
        FontMap.CopyTo(_memory, 0);
        program.CopyTo(_memory, ProgramMemoryOffset);
    }

    public void PerformCpuCycle(double delta)
    {
        // The number of instructions to run in this cycle.
        var numOfInstructions = (int)(CpuClock * delta);

        foreach (int _ in Enumerable.Range(0, numOfInstructions))
        {
            // Block execution until a key is pressed.
            if (_keyboardWaitKey.HasValue)
            {
                return;
            }

            var pc = _programCounter;
            var opcode = (ushort)(_memory[pc] << 8 | _memory[pc + 1]);

            var instrcution = OpcodeToInstruction(opcode);
            var next = RunInstruction(instrcution);

            Console.WriteLine($"DEBUG: {instrcution}");

            switch (next)
            {
                case Next:
                    _programCounter += 2;
                    break;

                case Skip:
                    _programCounter += 4;
                    break;

                case JumpTo(var address):
                    _programCounter = address;
                    break;
            }
        }
    }

    private ProgramCounterState RunInstruction(Instruction instruction)
    {
        // Execute instrcution and return the next program-counter.
        switch (instruction)
        {
            case ClearDisplay:
                {
                    Display.Clear();
                    return new Next();
                }

            case Return:
                {
                    _stackPointer -= 1;
                    var address = _stack[_stackPointer];
                    return new JumpTo(address);
                }

            case Jump(var address):
                {
                    return new JumpTo(address);
                }

            case Call(var address):
                {
                    _stack[_stackPointer] = (ushort)(_programCounter + 2);
                    _stackPointer += 1;
                    return new JumpTo(address);
                }

            case SkipIfEqualsByte(var register, var value):
                {
                    return ReadRegister(register) == value ? new Skip() : new Next();
                }

            case SkipIfNotEqualsByte(var register, var value):
                {
                    return ReadRegister(register) != value ? new Skip() : new Next();
                }

            case SkipIfEqual(var registerX, var registerY):
                {
                    return ReadRegister(registerX) == ReadRegister(registerY) ? new Skip() : new Next();
                }

            case LoadByte(var register, var value):
                {
                    LoadRegister(register, value);
                    return new Next();
                }

            case AddByte(var register, var value):
                {
                    // From my understanding "unchecked" will perform the addition and 
                    // not throw an exception if there is an overflow instead, the
                    // result wraps around.
                    var safeValue = unchecked(ReadRegister(register) + value);
                    LoadRegister(register, (byte)safeValue);
                    return new Next();
                }

            case Move(var registerX, var registerY):
                {
                    var value = ReadRegister(registerY);
                    LoadRegister(registerX, value);
                    return new Next();
                }

            case Or(var registerX, var registerY):
                {
                    var value = ReadRegister(registerX) | ReadRegister(registerY);
                    LoadRegister(registerX, (byte)value);
                    return new Next();
                }

            case And(var registerX, var registerY):
                {
                    var value = ReadRegister(registerX) & ReadRegister(registerY);
                    LoadRegister(registerX, (byte)value);
                    return new Next();
                }

            case Xor(var registerX, var registerY):
                {
                    var value = ReadRegister(registerX) ^ ReadRegister(registerY);
                    LoadRegister(registerX, (byte)value);
                    return new Next();
                }

            case Add(var registerX, var registerY):
                {
                    var value = ReadRegister(registerX) + ReadRegister(registerY);
                    LoadRegister(0xF, value > 255 ? (byte)1 : (byte)0);
                    LoadRegister(registerX, (byte)value);

                    return new Next();
                }

            case Sub(var registerX, var registerY):
                {
                    // From my understanding "unchecked" will perform the substitution
                    // andd not throw an exception if there is an overflow instead,
                    // the result wraps around.
                    var value = unchecked(ReadRegister(registerX) - ReadRegister(registerY));
                    var difference = ReadRegister(registerX) > ReadRegister(registerY);

                    LoadRegister(0xF, difference ? (byte)1 : (byte)0);
                    LoadRegister(registerX, (byte)value);

                    return new Next();
                }

            case ShiftRight(var register):
                {
                    var value = ReadRegister(register) >> 1;
                    LoadRegister(0xF, (byte)(ReadRegister(register) & 0b1));
                    LoadRegister(register, (byte)value);

                    return new Next();
                }

            case ReverseSub(var registerX, var registerY):
                {
                    // From my understanding "unchecked" will perform the substitution
                    // andd not throw an exception if there is an overflow instead,
                    // the result wraps around.
                    var value = unchecked(ReadRegister(registerY) - ReadRegister(registerX));
                    var difference = ReadRegister(registerY) > ReadRegister(registerX);

                    LoadRegister(0xF, difference ? (byte)1 : (byte)0);
                    LoadRegister(registerX, (byte)value);

                    return new Next();
                }

            case ShiftLeft(var register):
                {
                    var value = ReadRegister(register) << 1;
                    LoadRegister(0xF, (byte)(ReadRegister(register) >> 7));
                    LoadRegister(register, (byte)value);

                    return new Next();
                }

            case SkipIfNotEqual(var registerX, var registerY):
                {
                    return ReadRegister(registerX) != ReadRegister(registerY) ? new Skip() : new Next();
                }

            case LoadI(var value):
                {
                    _iRegister = value;
                    return new Next();
                }

            case JumpPlusZero(var address):
                {
                    var addr = address + ReadRegister(0);
                    return new JumpTo((ushort)addr);
                }

            case Random(var register, var value):
                {
                    var randomNum = (byte)_random.Next(0, 255);
                    LoadRegister(register, (byte)(randomNum & value));
                    return new Next();
                }

            case Draw(var registerX, var registerY, var offset):
                {
                    var sprite = _memory[_iRegister..(_iRegister + offset)];
                    var x = (int)ReadRegister(registerX);
                    var y = (int)ReadRegister(registerY);
                    var collision = Display.Draw(x, y, sprite);

                    LoadRegister(0xF, collision ? (byte)1 : (byte)0);
                    return new Next();
                }

            case SkipIfPressed(var register):
                {
                    return _keyboard[ReadRegister(register)] ? new Skip() : new Next();
                }

            case LoadDelayTimer(var register):
                {
                    LoadRegister(register, _delayRegister);
                    return new Next();
                }

            case WaitForKeyPress(var register):
                {
                    _keyboardWaitKey = register;
                    return new Next();
                }

            case SetDelayTimer(var register):
                {
                    _delayRegister = ReadRegister(register);
                    return new Next();
                }

            case SetSoundTimer(var register):
                {
                    _soundRegister = ReadRegister(register);
                    return new Next();
                }

            case AddToI(var register):
                {
                    _iRegister = ReadRegister(register);
                    return new Next();
                }

            case LoadSprite(var register):
                {
                    _iRegister = (ushort)(ReadRegister(register) * 5);
                    return new Next();
                }

            case BCDRepresentation(var register):
                {
                    var address = _iRegister;
                    var value = ReadRegister(register);

                    _memory[address] = (byte)(value / 100 % 10);
                    _memory[address + 1] = (byte)(value / 10 % 10);
                    _memory[address + 2] = (byte)(value % 10);

                    return new Next();
                }

            case StoreRegisters(var register):
                {
                    var limit = register + 1;
                    var address = _iRegister;

                    foreach (int offset in Enumerable.Range(0, limit))
                    {
                        _memory[address + offset] = ReadRegister((byte)offset);
                    }

                    return new Next();

                }

            case LoadRegisters(var register):
                {
                    var limit = register + 1;
                    var address = _iRegister;

                    foreach (int offset in Enumerable.Range(0, limit))
                    {
                        LoadRegister((byte)offset, _memory[address + offset]);
                    }

                    return new Next();
                }

            default:
                throw new Exception($"Instruction not implemented: {instruction}");
        }
    }

    private static Instruction OpcodeToInstruction(ushort opcode)
    {
        // Split the given opcode into seprate digits.
        var (d1, d2, d3, d4) = (
            (byte)(opcode >> 12),
            (byte)(opcode >> 8 & 0x0F),
            (byte)(opcode >> 4 & 0x00F),
            (byte)(opcode & 0x000F)
        );

        var address = (ushort)(opcode & 0x0FFF);
        var value = (byte)(opcode & 0x00FF);

        return d1 switch
        {
            0x0 => d4 switch
            {
                0x0 => new ClearDisplay(),
                0xE => new Return(),
                _ => throw new Exception($"Unknown opcode: 0x{opcode:X4}"),
            },
            0x1 => new Jump(address),
            0x2 => new Call(address),
            0x3 => new SkipIfEqualsByte(d2, value),
            0x4 => new SkipIfNotEqualsByte(d2, value),
            0x5 => new SkipIfEqual(d2, d3),
            0x6 => new LoadByte(d2, value),
            0x7 => new AddByte(d2, value),
            0x8 => d4 switch
            {
                0x0 => new Move(d2, d3),
                0x1 => new Or(d2, d3),
                0x2 => new And(d2, d3),
                0x3 => new Xor(d2, d3),
                0x4 => new Add(d2, d3),
                0x5 => new Sub(d2, d3),
                0x6 => new ShiftRight(d2),
                0x7 => new ReverseSub(d2, d3),
                0xE => new ShiftLeft(d2),
                _ => throw new Exception($"Unknown opcode: 0x{opcode:X4}"),
            },
            0x9 => new SkipIfNotEqual(d2, d3),
            0xA => new LoadI(address),
            0xB => new JumpPlusZero(address),
            0xC => new Random(d2, value),
            0xD => new Draw(d2, d3, d4),
            0xE => value switch
            {
                0x9E => new SkipIfNotPressed(d2),
                0xA1 => new SkipIfNotPressed(d2),
                _ => throw new Exception($"Unknown opcode: 0x{opcode:X4}"),
            },
            0xF => value switch
            {
                0x07 => new LoadDelayTimer(d2),
                0x0A => new WaitForKeyPress(d2),
                0x15 => new SetDelayTimer(d2),
                0x18 => new SetSoundTimer(d2),
                0x01 => new AddToI(d2),
                0x29 => new LoadSprite(d2),
                0x33 => new BCDRepresentation(d2),
                0x55 => new StoreRegisters(d2),
                0x65 => new LoadRegisters(d2),
                _ => throw new Exception($"Unknown opcode: 0x{opcode:X4}"),
            },
            _ => throw new Exception($"Unknown opcode: 0x{opcode:X4}"),
        };
    }

    private byte ReadRegister(byte index)
    {
        return _registers[index];
    }

    private void LoadRegister(byte index, byte value)
    {
        _registers[index] = value;
    }

    public void HandleKeyDown(int key)
    {
        _keyboard[key] = true;

        if (_keyboardWaitKey.HasValue)
        {
            LoadRegister(_keyboardWaitKey.Value, (byte)key);
            _keyboardWaitKey = null;
        }
    }

    public void HandleKeyUp(int key)
    {
        _keyboard[key] = false;
    }

    public void DecrementTimers()
    {
        if (_delayRegister > 0)
        {
            _delayRegister -= 1;
        }

        if (_soundRegister > 0)
        {
            _soundRegister -= 1;
        }
    }
}

class Display
{
    public const int DisplayWidth = 64;
    public const int DisplayHeight = 32;

    public byte[] Buffer = new byte[DisplayWidth * DisplayHeight];
    public bool BufferUpdate = false;

    public bool Draw(int x, int y, byte[] chunk)
    {
        var collision = false;
        BufferUpdate = true;

        for (int blockOffset = 0; blockOffset < chunk.Length; blockOffset++)
        {
            var block = chunk[blockOffset];
            var positionY = (y + blockOffset) % DisplayHeight;

            foreach (int pixelOffset in Enumerable.Range(0, 8))
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
