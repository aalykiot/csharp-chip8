// === MAIN ===

var path = "./roms/ibm-logo.ch8";
var program = File.ReadAllBytes(path);

var emulator = new Emulator(program);

emulator.PerformCpuCycle(0.05);

// === Chip8 Namespace ===

abstract record Instruction;

record ClearDisplay : Instruction;
record Return : Instruction;
record Jump(ushort address) : Instruction;
record Call(ushort address) : Instruction;
record SkipIfEqualsByte(byte register, byte value) : Instruction;
record SkipIfNotEqualsByte(byte register, byte value) : Instruction;
record SkipIfEqual(byte first, byte second) : Instruction;
record LoadByte(byte destination, byte value) : Instruction;
record AddByte(byte register, byte value) : Instruction;
record Move(byte destination, byte source) : Instruction;
record Or(byte destination, byte source) : Instruction;
record And(byte destination, byte source) : Instruction;
record Xor(byte destination, byte source) : Instruction;
record Add(byte destination, byte source) : Instruction;
record Sub(byte destination, byte source) : Instruction;
record ShiftRight(byte register) : Instruction;
record ReverseSub(byte destination, byte source) : Instruction;
record ShiftLeft(byte register) : Instruction;
record SkipIfNotEqual(byte first, byte second) : Instruction;
record LoadI(ushort address) : Instruction;
record JumpPlusZero(ushort address) : Instruction;
record Random(byte register, byte mask) : Instruction;
record Draw(byte registerX, byte registerY, byte height) : Instruction;
record SkipIfPressed(byte register) : Instruction;
record SkipIfNotPressed(byte register) : Instruction;
record LoadDelayTimer(byte register) : Instruction;
record WaitForKeyPress(byte register) : Instruction;
record SetDelayTimer(byte register) : Instruction;
record SetSoundTimer(byte register) : Instruction;
record AddToI(byte register) : Instruction;
record LoadSprite(byte register) : Instruction;
record BCDRepresentation(byte register) : Instruction;
record StoreRegisters(byte lastRegister) : Instruction;
record LoadRegisters(byte lastRegister) : Instruction;

abstract record ProgramCounter;

record Next : ProgramCounter;
record Skip : ProgramCounter;
record JumpTo(ushort address) : ProgramCounter;

record OpcodeDigits(byte a, byte b, byte c, byte d);

class Emulator
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
  private readonly Display _display = new();

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


  public Emulator(byte[] program)
  {
    // Load built-in fonts and the program into the emulator's memory (this 
    // can fail for some many reason but I'll ignore it for now).
    FontMap.CopyTo(this._memory, 0);
    program.CopyTo(this._memory, ProgramMemoryOffset);
  }

  public void PerformCpuCycle(double delta)
  {
    // The number of instructions to run in this cycle.
    var numOfInstructions = (int)(CpuClock * delta);

    foreach (int _ in Enumerable.Range(0, numOfInstructions))
    {
      // Block execution until a key is pressed.
      if (this._keyboardWaitKey.HasValue)
      {
        return;
      }

      var pc = this._programCounter;
      var opcode = (ushort)(this._memory[pc] << 8 | this._memory[pc + 1]);

      var instrcution = OpcodeToInstruction(opcode);
      var next = this.RunInstruction(instrcution);

      Console.WriteLine($"DEBUG: {instrcution}");

      switch (next)
      {
        case Next:
          this._programCounter += 2;
          break;

        case Skip:
          this._programCounter += 4;
          break;

        case JumpTo(var address):
          this._programCounter = address;
          break;
      }
    }
  }

  private ProgramCounter RunInstruction(Instruction instruction)
  {
    // Execute instrcution and return the next program-counter.
    switch (instruction)
    {
      case ClearDisplay:
        {
          this._display.Clear();
          return new Next();
        }

      case Return:
        {
          this._stackPointer -= 1;
          var address = this._stack[this._stackPointer];
          return new JumpTo(address);
        }

      case Jump(var address):
        {
          return new JumpTo(address);
        }

      case Call(var address):
        {
          this._stack[this._stackPointer] = (ushort)(this._programCounter + 2);
          this._stackPointer += 1;
          return new JumpTo(address);
        }

      case SkipIfEqualsByte(var register, var value):
        {
          return this.ReadRegister(register) == value ? new Skip() : new Next();
        }

      case SkipIfNotEqualsByte(var register, var value):
        {
          return this.ReadRegister(register) != value ? new Skip() : new Next();
        }

      case SkipIfEqual(var registerX, var registerY):
        {
          return this.ReadRegister(registerX) == this.ReadRegister(registerY) ? new Skip() : new Next();
        }

      case LoadByte(var register, var value):
        {
          this.LoadRegister(register, value);
          return new Next();
        }

      case AddByte(var register, var value):
        {
          // From my understanding "unchecked" will perform the addition and 
          // not throw an exception if there is an overflow instead, the
          // result wraps around.
          var safeValue = unchecked(this.ReadRegister(register) + value);
          this.LoadRegister(register, (byte)safeValue);
          return new Next();
        }

      case Move(var registerX, var registerY):
        {
          var value = this.ReadRegister(registerY);
          this.LoadRegister(registerX, value);
          return new Next();
        }

      case Or(var registerX, var registerY):
        {
          var value = this.ReadRegister(registerX) | this.ReadRegister(registerY);
          this.LoadRegister(registerX, (byte)value);
          return new Next();
        }

      case And(var registerX, var registerY):
        {
          var value = this.ReadRegister(registerX) & this.ReadRegister(registerY);
          this.LoadRegister(registerX, (byte)value);
          return new Next();
        }

      case Xor(var registerX, var registerY):
        {
          var value = this.ReadRegister(registerX) ^ this.ReadRegister(registerY);
          this.LoadRegister(registerX, (byte)value);
          return new Next();
        }

      case Add(var registerX, var registerY):
        {
          var value = (ushort)this.ReadRegister(registerX) + (ushort)this.ReadRegister(registerY);
          this.LoadRegister(0xF, value > 255 ? (byte)1 : (byte)0);
          this.LoadRegister(registerX, (byte)value);

          return new Next();
        }

      case Sub(var registerX, var registerY):
        {
          // From my understanding "unchecked" will perform the substitution
          // andd not throw an exception if there is an overflow instead,
          // the result wraps around.
          var value = unchecked(this.ReadRegister(registerX) - this.ReadRegister(registerY));
          var difference = this.ReadRegister(registerX) > this.ReadRegister(registerY);

          this.LoadRegister(0xF, difference ? (byte)1 : (byte)0);
          this.LoadRegister(registerX, (byte)value);

          return new Next();
        }

      case ShiftRight(var register):
        {
          var value = this.ReadRegister(register) >> 1;
          this.LoadRegister(0xF, (byte)(this.ReadRegister(register) & 0b1));
          this.LoadRegister(register, (byte)value);

          return new Next();
        }

      case ReverseSub(var registerX, var registerY):
        {
          // From my understanding "unchecked" will perform the substitution
          // andd not throw an exception if there is an overflow instead,
          // the result wraps around.
          var value = unchecked(this.ReadRegister(registerY) - this.ReadRegister(registerX));
          var difference = this.ReadRegister(registerY) > this.ReadRegister(registerX);

          this.LoadRegister(0xF, difference ? (byte)1 : (byte)0);
          this.LoadRegister(registerX, (byte)value);

          return new Next();
        }

      case ShiftLeft(var register):
        {
          var value = this.ReadRegister(register) << 1;
          this.LoadRegister(0xF, (byte)(this.ReadRegister(register) >> 7));
          this.LoadRegister(register, (byte)value);

          return new Next();
        }

      case SkipIfNotEqual(var registerX, var registerY):
        {
          return this.ReadRegister(registerX) != this.ReadRegister(registerY) ? new Skip() : new Next();
        }

      case LoadI(var value):
        {
          this._iRegister = value;
          return new Next();
        }

      case JumpPlusZero(var address):
        {
          var addr = address + (ushort)this.ReadRegister(0);
          return new JumpTo((ushort)addr);
        }

      case Random(var register, var value):
        {
          var randomNum = (byte)_random.Next(0, 255);
          this.LoadRegister(register, (byte)(randomNum & value));
          return new Next();
        }

      case Draw(var registerX, var registerY, var offset):
        {
          var sprite = this._memory[this._iRegister..(this._iRegister + offset)];
          var x = (int)this.ReadRegister(registerX);
          var y = (int)this.ReadRegister(registerY);
          var collision = this._display.Draw(x, y, sprite);

          this.LoadRegister(0xF, collision ? (byte)1 : (byte)0);
          return new Next();
        }

      case SkipIfPressed(var register):
        {
          return this._keyboard[this.ReadRegister(register)] ? new Skip() : new Next();
        }

      case LoadDelayTimer(var register):
        {
          this.LoadRegister(register, this._delayRegister);
          return new Next();
        }

      case WaitForKeyPress(var register):
        {
          this._keyboardWaitKey = register;
          return new Next();
        }

      case SetDelayTimer(var register):
        {
          this._delayRegister = this.ReadRegister(register);
          return new Next();
        }

      case SetSoundTimer(var register):
        {
          this._soundRegister = this.ReadRegister(register);
          return new Next();
        }

      case AddToI(var register):
        {
          this._iRegister = (ushort)this.ReadRegister(register);
          return new Next();
        }

      case LoadSprite(var register):
        {
          this._iRegister = (ushort)(this.ReadRegister(register) * 5);
          return new Next();
        }

      case BCDRepresentation(var register):
        {
          var address = this._iRegister;
          var value = this.ReadRegister(register);

          this._memory[address] = (byte)(value / 100 % 10);
          this._memory[address + 1] = (byte)(value / 10 % 10);
          this._memory[address + 2] = (byte)(value % 10);

          return new Next();
        }

      case StoreRegisters(var register):
        {
          var limit = register + 1;
          var address = this._iRegister;

          foreach (int offset in Enumerable.Range(0, limit))
          {
            this._memory[address + offset] = this.ReadRegister((byte)offset);
          }

          return new Next();

        }

      case LoadRegisters(var register):
        {
          var limit = register + 1;
          var address = this._iRegister;

          foreach (int offset in Enumerable.Range(0, limit))
          {
            this.LoadRegister((byte)offset, this._memory[address + offset]);
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
    var digits = new OpcodeDigits(
      (byte)(opcode >> 12),
      (byte)(opcode >> 8 & 0x0F),
      (byte)(opcode >> 4 & 0x00F),
      (byte)(opcode & 0x000F)
    );

    var (d1, d2, d3, d4) = digits;
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
    return this._registers[index];
  }

  private void LoadRegister(byte index, byte value)
  {
    this._registers[index] = value;
  }

  public void HandleKeyDown(int key)
  {
    this._keyboard[key] = true;

    if (this._keyboardWaitKey.HasValue)
    {
      this.LoadRegister(this._keyboardWaitKey.Value, (byte)key);
      this._keyboardWaitKey = null;
    }
  }

  public void HandleKeyUp(int key)
  {
    this._keyboard[key] = false;
  }

  public void DecrementTimers()
  {
    if (this._delayRegister > 0)
    {
      this._delayRegister -= 1;
    }

    if (this._soundRegister > 0)
    {
      this._soundRegister -= 1;
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
    this.BufferUpdate = true;

    for (int blockOffset = 0; blockOffset < chunk.Length; blockOffset++)
    {
      var block = chunk[blockOffset];
      var positionY = (y + blockOffset) % DisplayHeight;

      foreach (int pixelOffset in Enumerable.Range(0, 8))
      {
        // Get buffer index from x and y coordinates.
        var positionX = (x + pixelOffset) % DisplayWidth;
        var index = this.FromCoordinates(positionX, positionY);

        var bufferPixel = this.Buffer[index];
        var spritePixel = (block >> (7 - pixelOffset)) & 1;

        var pixel = (byte)(bufferPixel ^ spritePixel);
        this.Buffer[index] = pixel;

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
    this.Buffer = new byte[DisplayWidth * DisplayHeight];
  }

  private int FromCoordinates(int x, int y)
  {
    return (x + DisplayWidth * y);
  }
}