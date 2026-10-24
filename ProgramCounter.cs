namespace Chip8;

internal abstract record ProgramCounter;

internal record Next : ProgramCounter;
internal record Skip : ProgramCounter;
internal record JumpTo(ushort Address) : ProgramCounter;

