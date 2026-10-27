using System;

/// <summary>
/// Estado puro de seleccion de un minijuego (sin UI, sin red). Tres contratos:
/// - SimpleChoice   : radio (un indice).
/// - RequestAnalysis: multi-seleccion (mascara de bits).
/// - ServerRepair   : secuencia ordenada (empaquetada en un entero).
/// El valor final viaja en dos enteros: choice + mask.
/// </summary>
public sealed class MinigameSelection
{
    public const int MaxSequence = 4;
    private const byte Blank = 0;
    private const int PackBase = 1;

    private readonly byte[] sequence = new byte[MaxSequence];
    private int sequenceLength;
    private int mask;
    private int single = -1;

    public int Mask => mask;
    public int Single => single;
    public int SequenceLength => sequenceLength;

    public void Clear()
    {
        mask = 0;
        single = -1;
        sequenceLength = 0;
        for (int i = 0; i < sequence.Length; i++) sequence[i] = Blank;
    }

    public void Pick(MinigameDefinition def, int index)
    {
        if (def == null || index < 0) return;
        if (def.IsMultiSelect) ToggleMask(index);
        else if (def.IsSequence) ToggleSequence(index);
        else { single = index; mask = 1 << index; }
    }

    public bool IsPicked(int index)
    {
        if (index < 0) return false;
        if (single >= 0 && single == index) return true;
        if ((mask & (1 << index)) != 0) return true;
        return IndexOfSequence(index) >= 0;
    }

    public int SequencePosition(int index)
    {
        int at = IndexOfSequence(index);
        return at < 0 ? 0 : at + 1;
    }

    public int SequenceAt(int position)
    {
        if (position < 0 || position >= sequenceLength) return -1;
        return sequence[position];
    }

    public bool CanConfirm(MinigameDefinition def)
    {
        if (def == null) return false;
        if (def.IsMultiSelect) return mask != 0;
        if (def.IsSequence) return sequenceLength == RequiredLength(def);
        return single >= 0;
    }

    public int ChoiceValue(MinigameDefinition def)
    {
        if (def != null && def.IsSequence) return Pack(sequence, sequenceLength);
        return single;
    }

    /// <summary>Mascara que viaja en el RPC; solo tiene significado en multi-seleccion.</summary>
    public int MaskValue(MinigameDefinition def)
    {
        if (def != null && def.IsMultiSelect) return mask;
        return 0;
    }

    public static int RequiredLength(MinigameDefinition def)
    {
        if (def == null) return 0;
        if (def.requiredOrder != null && def.requiredOrder.Length > 0) return def.requiredOrder.Length;
        int correct = def.CountCorrect();
        return correct > 0 ? correct : 1;
    }

    private int IndexOfSequence(int index)
    {
        for (int i = 0; i < sequenceLength; i++) if (sequence[i] == index) return i;
        return -1;
    }

    private void ToggleMask(int index) { mask ^= 1 << index; }

    private void ToggleSequence(int index)
    {
        int at = IndexOfSequence(index);
        if (at >= 0)
        {
            for (int i = at; i < sequenceLength - 1; i++) sequence[i] = sequence[i + 1];
            sequenceLength--;
            sequence[sequenceLength] = Blank;
            return;
        }
        if (sequenceLength >= MaxSequence) return;
        sequence[sequenceLength++] = (byte)index;
    }

    /// <summary>Embalaje: cada entrada ocupa un byte en base (indice+1), 0 = fin.</summary>
    public static int Pack(byte[] entries, int length)
    {
        int packed = 0;
        if (entries == null) return packed;
        for (int i = 0; i < length && i < MaxSequence; i++)
        {
            int raw = entries[i] + PackBase;
            if (raw <= 0 || raw > 255) break;
            packed |= raw << (8 * i);
        }
        return packed;
    }

    /// <summary>Verifica que el paquete coincida exactamente con la secuencia requerida.</summary>
    public static bool MatchesOrder(int packed, int[] required)
    {
        if (required == null || required.Length == 0) return false;
        for (int i = 0; i < required.Length; i++)
        {
            int raw = (packed >> (8 * i)) & 0xFF;
            if (raw == 0) return false;
            if (raw - PackBase != required[i]) return false;
        }
        // 8*4 = 32 haria rotar el turno en C#; con 4 entradas ya se llena el entero.
        if (required.Length < MaxSequence)
        {
            int extra = (packed >> (8 * required.Length)) & 0xFF;
            if (extra != 0) return false;
        }
        return true;
    }

    /// <summary>Mascara de opciones correctas para seleccion multiple.</summary>
    public static int CorrectMask(MinigameDefinition def)
    {
        int want = 0;
        if (def == null || def.options == null) return want;
        for (int i = 0; i < def.options.Length && i < 32; i++)
            if (def.options[i] != null && def.options[i].isCorrect) want |= 1 << i;
        return want;
    }

    /// <summary>Primera opcion marcada de la mascara (util cuando choice no se envia).</summary>
    public static int FirstBit(int value)
    {
        if (value == 0) return -1;
        for (int i = 0; i < 32; i++) if ((value & (1 << i)) != 0) return i;
        return -1;
    }
}
