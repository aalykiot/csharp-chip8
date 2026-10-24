namespace Chip8;

internal abstract record Instruction;

internal record ClearDisplay : Instruction;
internal record Return : Instruction;
internal record Jump(ushort Address) : Instruction;
internal record Call(ushort Address) : Instruction;
internal record SkipIfEqualsByte(byte Register, byte Value) : Instruction;
internal record SkipIfNotEqualsByte(byte Register, byte Value) : Instruction;
internal record SkipIfEqual(byte First, byte Second) : Instruction;
internal record LoadByte(byte Destination, byte Value) : Instruction;
internal record AddByte(byte Register, byte Value) : Instruction;
internal record Move(byte Destination, byte Source) : Instruction;
internal record Or(byte Destination, byte Source) : Instruction;
internal record And(byte Destination, byte Source) : Instruction;
internal record Xor(byte Destination, byte Source) : Instruction;
internal record Add(byte Destination, byte Source) : Instruction;
internal record Sub(byte Destination, byte Source) : Instruction;
internal record ShiftRight(byte Register) : Instruction;
internal record ReverseSub(byte Destination, byte Source) : Instruction;
internal record ShiftLeft(byte Register) : Instruction;
internal record SkipIfNotEqual(byte First, byte Second) : Instruction;
internal record LoadI(ushort Address) : Instruction;
internal record JumpPlusZero(ushort Address) : Instruction;
internal record Random(byte Register, byte Mask) : Instruction;
internal record Draw(byte RegisterX, byte RegisterY, byte Height) : Instruction;
internal record SkipIfPressed(byte Register) : Instruction;
internal record SkipIfNotPressed(byte Register) : Instruction;
internal record LoadDelayTimer(byte Register) : Instruction;
internal record WaitForKeyPress(byte Register) : Instruction;
internal record SetDelayTimer(byte Register) : Instruction;
internal record SetSoundTimer(byte Register) : Instruction;
internal record AddToI(byte Register) : Instruction;
internal record LoadSprite(byte Register) : Instruction;
internal record BCDRepresentation(byte Register) : Instruction;
internal record StoreRegisters(byte Register) : Instruction;
internal record LoadRegisters(byte Register) : Instruction;

