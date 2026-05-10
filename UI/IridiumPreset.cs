using static Iridium.UI.IridiumLayout;

namespace Iridium.UI;

public static class IridiumPreset
{
    public static void OptionNameDescription(
        string name,
        bool description
    )
    {
        if (description)
        {
            Begin(ContainerDirection.Vertical, options: WidthMin);
            {
                Text(Localization.Get(name), options: WidthMin);
                Text(Localization.Get($"{name}.Description"), TextStyle.Secondary, WidthMin);
            }
            End();
        }
        else
        {
            Text(Localization.Get(name), options: WidthMin);
        }
    }

    public static void SwitchOption(
        Sizes sizes,
        ref bool option,
        string name,
        bool description = false
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            OptionNameDescription(name, description);
            Fill();
            Switch(ref option);
        }
        PopAlign();
        End();
    }

    public static void DoubleOption(
        Sizes sizes,
        ref double option,
        string name,
        IStructFormat<double>? format = null,
        bool description = false
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            OptionNameDescription(name, description);
            Fill();
            StructField(ref option, format ?? DoubleFormat(), WidthMin);
        }
        PopAlign();
        End();
    }

    public static void IntOption(
        Sizes sizes,
        ref int option,
        string name,
        IStructFormat<int>? format = null,
        bool description = false
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            OptionNameDescription(name, description);
            Fill();
            StructField(ref option, format ?? IntFormat(), WidthMin);
        }
        PopAlign();
        End();
    }

    public static void TextOption(
        Sizes sizes,
        ref string option,
        string name,
        bool description = false
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            OptionNameDescription(name, description);
            Fill();
            TextField(ref option, options: WidthMin);
        }
        PopAlign();
        End();
    }

    public static void CheckboxTextOption(
        Sizes sizes,
        ref bool enabled,
        ref string option,
        string name,
        bool description = false
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            Checkbox(ref enabled);
            OptionNameDescription(name, description);
            Fill();
            TextField(ref option, options: WidthMin);
        }
        PopAlign();
        End();
    }

    public static void CheckboxSwitchOption(
        Sizes sizes,
        ref bool enabled,
        ref bool option,
        string name,
        bool description = false
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            Checkbox(ref enabled);
            OptionNameDescription(name, description);
            Fill();
            Switch(ref option, WidthMin);
        }
        PopAlign();
        End();
    }

    public static void CheckboxDoubleOption(
        Sizes sizes,
        ref bool enabled,
        ref double option,
        string name,
        bool description = false,
        IStructFormat<double>? format = null
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            Checkbox(ref enabled);
            OptionNameDescription(name, description);
            Fill();
            StructField(ref option, format ?? DoubleFormat(), WidthMin);
        }
        PopAlign();
        End();
    }

    public static void CheckboxIntOption(
        Sizes sizes,
        ref bool enabled,
        ref int option,
        string name,
        bool description = false,
        IStructFormat<int>? format = null
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            Checkbox(ref enabled);
            OptionNameDescription(name, description);
            Fill();
            StructField(ref option, format ?? IntFormat(), WidthMin);
        }
        PopAlign();
        End();
    }

    public static bool IconText(
        Sizes sizes,
        IconStyle icon,
        string text
    )
    {
        var result = false;

        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            result |= Icon(icon);
            result |= Text(Localization.Get(text), options: WidthMax);
        }
        PopAlign();
        End();

        return result;
    }

    public static bool Collapse(
        Sizes sizes,
        ref bool expanded,
        string text,
        TextStyle style = TextStyle.Normal
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            if (ArrowButton(expanded ? ArrowStyle.Down : ArrowStyle.Right)) expanded = !expanded;
            Text(Localization.Get(text), style, WidthMax);
        }
        PopAlign();
        End();

        return expanded;
    }

    public static void SelectorOption(
        Sizes sizes,
        ref int selected,
        string[] selections,
        string name,
        bool description = false
    )
    {
        Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
        PushAlign(0.5);
        {
            OptionNameDescription(name, description);
            Fill();
            Selector(ref selected, selections, options: WidthMin);
        }
        PopAlign();
        End();
    }
}