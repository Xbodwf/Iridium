using System;
using static Iridium.UI.IridiumLayout;

namespace Iridium.UI;

public static class IridiumPreset
{
    public static Element OptionNameDescription(
        string name,
        bool description
    )
    {
        if (description)
        {
            return VBox(
                ContainerStyle.None,
                null,
                Text(Localization.Get(name), TextStyle.Normal, WidthMin),
                Text(Localization.Get($"{name}.Description"), TextStyle.Secondary, WidthMin)
            );
        }
        else
        {
            return Text(Localization.Get(name), TextStyle.Normal, WidthMin);
        }
    }

    public static Element SwitchOption(
        Sizes sizes,
        bool option,
        Action<bool> onChanged,
        string name,
        bool description = false
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                OptionNameDescription(name, description),
                Fill(),
                Switch(option, onChanged, WidthMin)
            )
        );
    }

    public static Element DoubleOption(
        Sizes sizes,
        double option,
        Action<double> onChanged,
        string name,
        IStructFormat<double>? format = null,
        bool description = false
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                OptionNameDescription(name, description),
                Fill(),
                StructField(option, format ?? DoubleFormat(), onChanged, WidthMin)
            )
        );
    }

    public static Element IntOption(
        Sizes sizes,
        int option,
        Action<int> onChanged,
        string name,
        IStructFormat<int>? format = null,
        bool description = false
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                OptionNameDescription(name, description),
                Fill(),
                StructField(option, format ?? IntFormat(), onChanged, WidthMin)
            )
        );
    }

    public static Element TextOption(
        Sizes sizes,
        string? option,
        Action<string?> onChanged,
        string name,
        bool description = false
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                OptionNameDescription(name, description),
                Fill(),
                TextField(option ?? string.Empty, onChanged, null, WidthMin)
            )
        );
    }

    public static Element CheckboxTextOption(
        Sizes sizes,
        bool enabled,
        Action<bool> onEnabledChanged,
        string? option,
        Action<string?> onOptionChanged,
        string name,
        bool description = false
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                Checkbox(enabled, onEnabledChanged),
                OptionNameDescription(name, description),
                Fill(),
                TextField(option ?? string.Empty, onOptionChanged, null, WidthMin)
            )
        );
    }

    public static Element CheckboxSwitchOption(
        Sizes sizes,
        bool enabled,
        Action<bool> onEnabledChanged,
        bool option,
        Action<bool> onOptionChanged,
        string name,
        bool description = false
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                Checkbox(enabled, onEnabledChanged),
                OptionNameDescription(name, description),
                Fill(),
                Switch(option, onOptionChanged, WidthMin)
            )
        );
    }

    public static Element CheckboxDoubleOption(
        Sizes sizes,
        bool enabled,
        Action<bool> onEnabledChanged,
        double option,
        Action<double> onOptionChanged,
        string name,
        bool description = false,
        IStructFormat<double>? format = null
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                Checkbox(enabled, onEnabledChanged),
                OptionNameDescription(name, description),
                Fill(),
                StructField(option, format ?? DoubleFormat(), onOptionChanged, WidthMin)
            )
        );
    }

    public static Element CheckboxIntOption(
        Sizes sizes,
        bool enabled,
        Action<bool> onEnabledChanged,
        int option,
        Action<int> onOptionChanged,
        string name,
        bool description = false,
        IStructFormat<int>? format = null
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                Checkbox(enabled, onEnabledChanged),
                OptionNameDescription(name, description),
                Fill(),
                StructField(option, format ?? IntFormat(), onOptionChanged, WidthMin)
            )
        );
    }

    public static Element IconText(
        Sizes sizes,
        IconStyle icon,
        string text,
        Action? onClick = null
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                Icon(icon, onClick, WidthMin),
                Text(Localization.Get(text), TextStyle.Normal, WidthMax)
            )
        );
    }

    public static Element IconTextFormatted(
        Sizes sizes,
        IconStyle icon,
        string text,
        params object[] args
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                Icon(icon, null, WidthMin),
                Text(string.Format(Localization.Get(text), args), TextStyle.Normal, WidthMax)
            )
        );
    }

    public static Element Collapse(
        Sizes sizes,
        bool expanded,
        Action<bool> onExpandedChanged,
        string text,
        TextStyle style = TextStyle.Normal
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                ArrowButton(
                    expanded ? ArrowStyle.Down : ArrowStyle.Right,
                    () => onExpandedChanged(!expanded),
                    WidthMin
                ),
                Text(Localization.Get(text), style, WidthMax)
            )
        );
    }

    public static Element SelectorOption(
        Sizes sizes,
        int selected,
        Action<int> onSelected,
        string[] selections,
        string name,
        bool description = false
    )
    {
        return HBox(
            ContainerStyle.None,
            sizes,
            WidthMax,
            Align(
                0.5,
                0,
                OptionNameDescription(name, description),
                Fill(),
                Selector(selected, selections, onSelected, ButtonStyle.Element, ButtonStyle.Primary, WidthMin)
            )
        );
    }
}
