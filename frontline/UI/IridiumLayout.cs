using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityModManagerNet;
using Iridium.Utilities;
using Color = UnityEngine.Color;
using FontStyle = UnityEngine.FontStyle;
using Graphics = System.Drawing.Graphics;
using Iridium.Polyfill;

namespace Iridium.UI;

public static class IridiumLayout
{
    public enum ArrowStyle
    {
        Right,
        Down,
        Left,
        Up
    }

    public enum ButtonStyle
    {
        Element,
        Primary
    }

    public enum ContainerDirection
    {
        Horizontal,
        Vertical
    }

    public enum ContainerStyle
    {
        None,
        Padding,
        Background
    }

    public enum IconStyle
    {
        Information,
        Success,
        Warning,
        Error
    }

    public enum TextStyle
    {
        Normal,
        Subtitle,
        Title,
        Secondary
    }

    private static readonly Trigger<int, ResolutionResources> ResolutionTrigger = new();

    private static ResolutionResources Resolution => ResolutionTrigger.Get(
        UnityModManager.UI.Scale(1048576),
        scaleTimes1M => new ResolutionResources(scaleTimes1M)
    );

    internal static List<ContainerDirection> ContainerStack { get; } = [ContainerDirection.Vertical];

    private static List<int> ElementCountStack { get; } = [0];

    private static List<bool> IsBackgroundStack { get; } = [false];

    private static List<bool> ApplyPreMarginHorizontalStack { get; } = [false];

    private static List<bool> ApplyPreMarginVerticalStack { get; } = [false];

    private static List<(double?, Sizes)?> SizesStack { get; } = [null];

    private static List<(double, double)?> AlignmentStack { get; } = [null];

    private static double LastMargin { get; set; }

    private static bool IsBackground1 { get; set; }

    public static GUILayoutOption WidthMin => GUILayout.ExpandWidth(false);

    public static GUILayoutOption WidthMax => GUILayout.ExpandWidth(true);

    public static void EnsureTexturesAlive()
    {
        if (Resolution.Textures.Any(x => x == null))
        {
            var oldResources = ResolutionTrigger.ResetWithOld();
            if (oldResources != null)
                oldResources.DestroyTextures();
        }
    }

    private static GUIStyle AdjustMargin(GUIStyle style)
    {
        var copied = new GUIStyle(style);
        var elementCount = ElementCountStack[^1];
        ElementCountStack[^1] = elementCount + 1;
        var containerDirection = ContainerStack[^1];
        var applyPreMargin = elementCount > 0 || (containerDirection == ContainerDirection.Horizontal
            ? ApplyPreMarginHorizontalStack[^1]
            : ApplyPreMarginVerticalStack[^1]);

        var margin = (int)((elementCount > 0 ? Resolution.Margin : 0) + LastMargin);

        var addition = !applyPreMargin
            ? new RectOffset(0, 0, 0, 0)
            : containerDirection switch
            {
                ContainerDirection.Horizontal => new RectOffset(margin, 0, 0, 0),
                ContainerDirection.Vertical => new RectOffset(0, 0, margin, 0),
                _ => new RectOffset(0, 0, 0, 0)
            };

        copied.margin = new RectOffset(
            copied.margin.left + addition.left,
            copied.margin.right + addition.right,
            copied.margin.top + addition.top,
            copied.margin.bottom + addition.bottom
        );

        if (!applyPreMargin)
        {
            if (containerDirection == ContainerDirection.Horizontal) copied.margin.left = 0;
            else if (containerDirection == ContainerDirection.Vertical) copied.margin.top = 0;
        }

        var sizesElement = SizesStack[^1];
        var alignElement = AlignmentStack[^1];

        if (sizesElement is not null)
        {
            var (maxSize, sizes) = sizesElement.Value;
            var beginningMargin = Math.Max(
                0,
                containerDirection switch
                {
                    ContainerDirection.Horizontal => copied.margin.top,
                    ContainerDirection.Vertical => copied.margin.left,
                    _ => 0
                }
            );
            sizes.NextMaxMargin = Math.Max(0, beginningMargin);

            if (alignElement is not null)
            {
                var size = sizes.Next;
                var (ratio, offset) = alignElement.Value;
                if (maxSize is not null && size is not null)
                {
                    var space = Math.Max(0, maxSize.Value - size.Value);
                    var marginAddition = (int)Math.Floor(Math.Max(0, space * ratio + offset + sizes.MaxMargin - beginningMargin));
                    if (containerDirection == ContainerDirection.Horizontal) copied.margin.top += marginAddition;
                    else if (containerDirection == ContainerDirection.Vertical) copied.margin.left += marginAddition;
                }
            }
        }

        LastMargin = containerDirection switch
        {
            ContainerDirection.Horizontal => copied.margin.right,
            ContainerDirection.Vertical => copied.margin.bottom,
            _ => 0
        };

        return copied;
    }

    private static GUILayoutOption[] Options(object[] options)
    {
        return options.OfType<GUILayoutOption>().Append(GUILayout.ExpandHeight(false)).ToArray();
    }

    public static void AddMargin(
        double size
    )
    {
        LastMargin += Resolution.Scaled(size);
    }

    public static void Space(
        double size
    )
    {
        GUILayout.Space((float)Resolution.Scaled(size));
    }

    public static void Fill()
    {
        var containerDirection = ContainerStack[^1];
        if (containerDirection != ContainerDirection.Horizontal)
            throw new InvalidOperationException("Fill can only be used in Horizontal containers");
        GUILayout.FlexibleSpace();
    }

    public static void PushSizes(Sizes? sizes = null)
    {
        if (sizes is null)
        {
            SizesStack.Add(null);
            return;
        }

        sizes.Begin();
        SizesStack.Add((sizes.Max, sizes));
    }

    public static void UpdateMaxSize()
    {
        if (Event.current.type != EventType.Repaint) return;
        var sizesElement = SizesStack[^1];
        if (sizesElement is null) return;
        var (_, sizes) = sizesElement.Value;
        var rect = GUILayoutUtility.GetLastRect();
        var containerDirection = ContainerStack[^1];
        var size = containerDirection == ContainerDirection.Horizontal ? rect.height : rect.width;
        sizes.Put(Math.Max(0, size));
    }

    public static void Begin(
        ContainerDirection direction,
        ContainerStyle style = ContainerStyle.None,
        Sizes? sizes = null,
        params object[] options
    )
    {
        if (style == ContainerStyle.Background) IsBackground1 = !IsBackground1;

        var guiStyle = AdjustMargin(style switch
        {
            ContainerStyle.None => Resolution.Container,
            ContainerStyle.Padding => Resolution.PaddingContainer,
            ContainerStyle.Background => IsBackground1
                ? Resolution.Background1Container
                : Resolution.Background0Container,
            _ => Resolution.Container
        });

        if (direction == ContainerDirection.Horizontal) GUILayout.BeginHorizontal(guiStyle, Options(options));
        else if (direction == ContainerDirection.Vertical) GUILayout.BeginVertical(guiStyle, Options(options));

        LastMargin = 0;
        ContainerStack.Add(direction);
        ElementCountStack.Add(0);
        IsBackgroundStack.Add(style == ContainerStyle.Background);
        PushSizes(sizes);

        if (style == ContainerStyle.None)
        {
            ApplyPreMarginHorizontalStack.Add(ApplyPreMarginHorizontalStack[^1]);
            ApplyPreMarginVerticalStack.Add(ApplyPreMarginVerticalStack[^1]);
        }
        else
        {
            ApplyPreMarginHorizontalStack.Add(false);
            ApplyPreMarginVerticalStack.Add(false);
        }
    }

    public static void End()
    {
        var direction = ContainerStack[^1];
        if (IsBackgroundStack[^1]) IsBackground1 = !IsBackground1;

        LastMargin = 0;

        if (direction == ContainerDirection.Horizontal)
        {
            GUILayout.EndHorizontal();
            UpdateMaxSize();
            // Remove current container entries after UpdateMaxSize
            ContainerStack.RemoveAt(ContainerStack.Count - 1);
            ElementCountStack.RemoveAt(ElementCountStack.Count - 1);
            ApplyPreMarginHorizontalStack.RemoveAt(ApplyPreMarginHorizontalStack.Count - 1);
            ApplyPreMarginVerticalStack.RemoveAt(ApplyPreMarginVerticalStack.Count - 1);
            IsBackgroundStack.RemoveAt(IsBackgroundStack.Count - 1);
            SizesStack.RemoveAt(SizesStack.Count - 1);
            ApplyPreMarginHorizontalStack[^1] = true;
        }
        else if (direction == ContainerDirection.Vertical)
        {
            GUILayout.EndVertical();
            UpdateMaxSize();
            // Remove current container entries after UpdateMaxSize
            ContainerStack.RemoveAt(ContainerStack.Count - 1);
            ElementCountStack.RemoveAt(ElementCountStack.Count - 1);
            ApplyPreMarginHorizontalStack.RemoveAt(ApplyPreMarginHorizontalStack.Count - 1);
            ApplyPreMarginVerticalStack.RemoveAt(ApplyPreMarginVerticalStack.Count - 1);
            IsBackgroundStack.RemoveAt(IsBackgroundStack.Count - 1);
            SizesStack.RemoveAt(SizesStack.Count - 1);
            ApplyPreMarginVerticalStack[^1] = true;
        }
    }

    public static void PushAlign(double ratio = 0, double offset = 0)
    {
        AlignmentStack.Add((ratio, offset));
    }

    public static void PushNoAlign()
    {
        AlignmentStack.Add(null);
    }

    public static void PopAlign()
    {
        AlignmentStack.RemoveAt(AlignmentStack.Count - 1);
    }

    public static void Separator(
        params object[] options
    )
    {
        var direction = ContainerStack[^1];

        if (direction == ContainerDirection.Horizontal)
        {
            GUILayout.Label(
                GUIContent.none,
                AdjustMargin(Resolution.VerticalSeparator),
                Options(options)
            );
            UpdateMaxSize();
        }
        else if (direction == ContainerDirection.Vertical)
        {
            GUILayout.Label(
                GUIContent.none,
                AdjustMargin(Resolution.HorizontalSeparator),
                Options(options)
            );
            UpdateMaxSize();
        }
    }

    public static bool Text(
        string text,
        TextStyle style = TextStyle.Normal,
        params object[] options
    )
    {
        var result = GUILayout.Button(
            text,
            AdjustMargin(style switch
            {
                TextStyle.Normal => Resolution.NormalText,
                TextStyle.Subtitle => Resolution.SubtitleText,
                TextStyle.Title => Resolution.TitleText,
                TextStyle.Secondary => Resolution.SecondaryText,
                _ => Resolution.NormalText
            }),
            Options(options)
        );
        UpdateMaxSize();
        return result;
    }

    public static bool Button(
        string text,
        ButtonStyle style = ButtonStyle.Primary,
        params object[] options
    )
    {
        var result = GUILayout.Button(
            text,
            AdjustMargin(style switch
            {
                ButtonStyle.Element => Resolution.ElementButton,
                ButtonStyle.Primary => Resolution.PrimaryButton,
                _ => Resolution.ElementButton
            }),
            Options(options)
        );
        UpdateMaxSize();
        return result;
    }

    public static bool? Checkbox(
        bool on,
        params object[] options
    )
    {
        bool? result = null;

        if (
            GUILayout.Button(
                GUIContent.none,
                AdjustMargin(on ? Resolution.CheckboxOn : Resolution.CheckboxOff),
                Options(options)
            )
        ) result = !on;
        UpdateMaxSize();
        return result;
    }

    public static bool? Checkbox(
        ref bool on,
        params object[] options
    )
    {
        var result = Checkbox(on, options);
        if (result is not null) on = result.Value;
        return result;
    }

    public static bool ArrowButton(
        ArrowStyle style,
        params object[] options
    )
    {
        var result = GUILayout.Button(
            GUIContent.none,
            AdjustMargin(style switch
            {
                ArrowStyle.Right => Resolution.ArrowButtonRight,
                ArrowStyle.Down => Resolution.ArrowButtonDown,
                ArrowStyle.Left => Resolution.ArrowButtonLeft,
                ArrowStyle.Up => Resolution.ArrowButtonUp,
                _ => Resolution.ArrowButtonRight
            }),
            Options(options)
        );
        UpdateMaxSize();
        return result;
    }

    public static bool? Switch(
        bool on,
        params object[] options
    )
    {
        bool? result = null;

        if (
            GUILayout.Button(
                GUIContent.none,
                AdjustMargin(on ? Resolution.SwitchOn : Resolution.SwitchOff),
                Options(options)
            )
        ) result = !on;
        UpdateMaxSize();

        return result;
    }

    public static bool? Switch(
        ref bool on,
        params object[] options
    )
    {
        var result = Switch(on, options);
        if (result is not null)
        {
            on = result.Value;
            GUI.changed = true;
        }
        return result;
    }

    public static int? Selector(
        int selected,
        IReadOnlyList<string> selections,
        ButtonStyle style = ButtonStyle.Element,
        ButtonStyle styleSelected = ButtonStyle.Primary,
        params object[] options
    )
    {
        int? result = null;

        for (var i = 0; i < selections.Count; i++)
            if (Button(selections[i], i == selected ? styleSelected : style, options))
                result = i;

        return result;
    }

    public static string? Selector(
        string selected,
        IReadOnlyList<(string, string)> selections,
        ButtonStyle style = ButtonStyle.Element,
        ButtonStyle styleSelected = ButtonStyle.Primary,
        params object[] options
    )
    {
        string? result = null;

        foreach (var (key, name) in selections)
            if (Button(name, key == selected ? styleSelected : style, options))
                result = key;

        return result;
    }

    public static int? Selector(
        ref int selected,
        IReadOnlyList<string> selections,
        ButtonStyle style = ButtonStyle.Element,
        ButtonStyle styleSelected = ButtonStyle.Primary,
        params object[] options
    )
    {
        var result = Selector(selected, selections, style, styleSelected, options);
        if (result is not null)
        {
            selected = result.Value;
            GUI.changed = true;
        }
        return result;
    }

    public static string? Selector(
        ref string selected,
        IReadOnlyList<(string, string)> selections,
        ButtonStyle style = ButtonStyle.Element,
        ButtonStyle styleSelected = ButtonStyle.Primary,
        params object[] options
    )
    {
        var result = Selector(selected, selections, style, styleSelected, options);
        if (result is not null)
        {
            selected = result;
            GUI.changed = true;
        }
        return result;
    }

    public static string? TextField(
        string content,
        int? maxLength = null,
        params object[] options
    )
    {
        string? result = null;
        string newContent;

        if (
            (newContent = GUILayout.TextField(
                content,
                maxLength ?? -1,
                AdjustMargin(Resolution.TextField),
                Options(options)
            )) != content
        ) result = newContent;
        UpdateMaxSize();

        return result;
    }

    public static string? TextField(
        ref string? content,
        int? maxLength = null,
        params object[] options
    )
    {
        var result = TextField(content ?? string.Empty, maxLength, options);
        if (result is not null) content = result;
        return result;
    }

    public static T? ClassField<T>(
        T content,
        IClassFormat<T> format,
        params object[] options
    ) where T : class
    {
        var oldContent = format.Format(content);
        var newContent = TextField(oldContent, null, options);
        if (newContent is null) return null;
        var newValue = format.Parse(newContent);
        return newValue;
    }

    public static T? ClassField<T>(
        ref T content,
        IClassFormat<T> format,
        params object[] options
    ) where T : class
    {
        var result = ClassField(content, format, options);
        if (result is not null) content = result;
        return result;
    }

    public static T? StructField<T>(
        T content,
        IStructFormat<T> format,
        params object[] options
    ) where T : struct
    {
        var oldContent = format.Format(content);
        var newContent = TextField(
            oldContent,
            null,
            options
        );
        if (newContent is null) return null;
        var newValue = format.Parse(newContent);
        return newValue;
    }

    public static T? StructField<T>(
        ref T content,
        IStructFormat<T> format,
        params object[] options
    ) where T : struct
    {
        var result = StructField(content, format, options);
        if (result is not null) content = result.Value;
        return result;
    }

    public static bool Icon(
        IconStyle style = IconStyle.Information,
        params object[] options
    )
    {
        var result = GUILayout.Button(
            GUIContent.none,
            AdjustMargin(style switch
            {
                IconStyle.Information => Resolution.IconInformation,
                IconStyle.Success => Resolution.IconSuccess,
                IconStyle.Warning => Resolution.IconWarning,
                IconStyle.Error => Resolution.IconError,
                _ => Resolution.IconInformation
            }),
            Options(options)
        );
        UpdateMaxSize();
        return result;
    }

    public static GUILayoutOption Width(double width)
    {
        return GUILayout.Width((float)Resolution.Scaled(width));
    }

    public static GUILayoutOption Height(double height)
    {
        return GUILayout.Height((float)Resolution.Scaled(height));
    }

    public static GUILayoutOption MinWidth(double width)
    {
        return GUILayout.MinWidth((float)Resolution.Scaled(width));
    }

    public static GUILayoutOption MaxWidth(double width)
    {
        return GUILayout.MaxWidth((float)Resolution.Scaled(width));
    }

    public static GUILayoutOption MinHeight(double height)
    {
        return GUILayout.MinHeight((float)Resolution.Scaled(height));
    }

    public static GUILayoutOption MaxHeight(double height)
    {
        return GUILayout.MaxHeight((float)Resolution.Scaled(height));
    }

    public static IStructFormat<double> DoubleFormat(
        int? precision = null,
        double min = double.NegativeInfinity,
        double max = double.PositiveInfinity
    )
    {
        return new DoubleFormatImpl(precision, min, max);
    }

    public static IStructFormat<int> IntFormat(
        int min = int.MinValue,
        int max = int.MaxValue
    )
    {
        return new IntFormatImpl(min, max);
    }

    public interface IClassFormat<T> where T : class
    {
        string Format(T value);

        T? Parse(string text);
    }

    public interface IStructFormat<T> where T : struct
    {
        string Format(T value);

        T? Parse(string text);
    }

    private class DoubleFormatImpl(
        int? precision,
        double min,
        double max
    ) : IStructFormat<double>
    {
        public string Format(double value)
        {
            if (precision is not null) return value.ToString($"F{precision}");
            var formatted = $"{value:R}";
            if (formatted.Contains('.') || !Polyfill.Double.IsFinite(value)) return formatted;
            var indexToAddDot = formatted.IndexOfAny(['e', 'E']);
            if (indexToAddDot < 0) indexToAddDot = formatted.Length;
            return formatted.Insert(indexToAddDot, ".0");
        }

        public double? Parse(string text)
        {
            if (text.IsNullOrEmpty()) return 0;
            if (!double.TryParse(text, out var result)) return null;
            result = Polyfill.MathI.Clamp(result, min, max);
            return result;
        }
    }

    private class IntFormatImpl(
        int min,
        int max
    ) : IStructFormat<int>
    {
        public string Format(int value)
        {
            return value.ToString();
        }

        public int? Parse(string text)
        {
            if (text.IsNullOrEmpty()) return 0;
            if (!int.TryParse(text, out var result)) return null;
            result = Polyfill.MathI.Clamp(result, min, max);
            return result;
        }
    }

    public class Sizes
    {
        private int ReadPointer { get; set; }

        private int WritePointer { get; set; }

        private List<double> SizeList { get; } = [];

        public int MaxMargin { get; private set; }

        public int NextMaxMargin { get; set; }

        public double? Max => SizeList.Count == 0 ? null : SizeList.Max();

        public double? Next => ReadPointer < SizeList.Count ? SizeList[ReadPointer++] : null;

        public void Begin()
        {
            ReadPointer = 0;
            WritePointer = 0;
            MaxMargin = NextMaxMargin;
            NextMaxMargin = 0;
        }

        public void Put(double value)
        {
            if (WritePointer < SizeList.Count) SizeList[WritePointer] = value;
            else SizeList.Add(value);
            ++WritePointer;
        }
    }

    public class SizesGroup
    {
        private SizesGroup()
        {
        }

        private int SizesPointer { get; set; }

        private int GroupPointer { get; set; }

        private List<Sizes> SizesList { get; } = [];

        private List<SizesGroup> Groups { get; } = [];

        public Sizes Sizes
        {
            get
            {
                while (SizesPointer >= SizesList.Count) SizesList.Add(new Sizes());
                return SizesList[SizesPointer++];
            }
        }

        public SizesGroup Group
        {
            get
            {
                while (GroupPointer >= Groups.Count) Groups.Add(new SizesGroup());
                var group = Groups[GroupPointer++];
                group.Begin();
                return group;
            }
        }

        public void Begin()
        {
            SizesPointer = 0;
            GroupPointer = 0;
        }

        public static implicit operator Sizes(SizesGroup group)
        {
            return group.Sizes;
        }

        public class Holder
        {
            private SizesGroup Group { get; } = new();

            public SizesGroup Begin()
            {
                Group.Begin();
                return Group;
            }
        }
    }

    private class ResolutionResources
    {
        private const double BaseTextSize = 12;

        private const double SubtitleTextSize = 18;

        private const double TitleTextSize = 24;

        private const double SecondaryTextSize = BaseTextSize;

        private const double BaseMargin = 8;

        private const double SubtitleAdditionalMargin = 4;

        private const double TitleAdditionalMargin = 8;

        private const double ContainerPadding = 8;

        private const double BackgroundRadius = 16;

        private const double ButtonRadius = 8;

        private const double SquareIconSize = 20;

        private const double SquareIconRadius = 4;

        private const double SquareIconBorder = 1;

        private const double SwitchWidth = 36;

        private const double SwitchHeight = 20;

        private const double SwitchButtonRadius = 7;

        private const double TextFieldRadius = 8;

        private const double TextFieldBorder = 1;

        private const double IconSize = 20;

        private const double IconBorder = 2;

        private static readonly ColorGroup Background0Colors = new(RGB(0x151617));

        private static readonly ColorGroup Background1Colors = new(RGB(0x0D0E0F));

        private static readonly ColorGroup SeparatorColors = new(ARGB(0x20FFFFFF));

        private static readonly ColorGroup PrimaryColors = new(RGB(0xD973A5), RGB(0xC16693), RGB(0x9C5277));

        private static readonly ColorGroup ElementColors = new(RGB(0x313338), RGB(0x373B45), RGB(0x30333C));

        private static readonly ColorGroup ElementBorderColors = new(RGB(0x494F5C));

        private static readonly ColorGroup NormalTextColors = new(RGB(0xE9ECEF));

        private static readonly ColorGroup SubtitleTextColors = new(RGB(0xF1F3F5));

        private static readonly ColorGroup TitleTextColors = new(RGB(0xF8F9FA));

        private static readonly ColorGroup SecondaryTextColors = new(RGB(0x7D7E7F));

        private static readonly ColorGroup CheckboxOffColors = ElementColors;

        private static readonly ColorGroup CheckboxOffBorderColors = ElementBorderColors;

        private static readonly ColorGroup CheckboxOnColors = new(PrimaryColors.Normal);

        private static readonly ColorGroup CheckboxOnBorderColors = PrimaryColors;

        private static readonly ColorGroup CheckboxCheckmarkColors = TitleTextColors;

        private static readonly ColorGroup ArrowButtonColors = ElementColors;

        private static readonly ColorGroup ArrowButtonBorderColors = ElementBorderColors;

        private static readonly ColorGroup ArrowButtonArrowColors = TitleTextColors;

        private static readonly ColorGroup SwitchOffColors = ElementColors;

        private static readonly ColorGroup SwitchOnColors = PrimaryColors;

        private static readonly ColorGroup SwitchButtonColors = TitleTextColors;

        private static readonly ColorGroup TextFieldColors = new(RGB(0x151719));

        private static readonly ColorGroup TextFieldBorderColors = new(RGB(0x222326), RGB(0x222326), PrimaryColors.Normal, PrimaryColors.Normal);

        private static readonly ColorGroup IconInformationColors = ElementBorderColors;

        private static readonly ColorGroup IconInformationBorderColors = new(ElementColors.Hovered);

        private static readonly ColorGroup IconSuccessColors = new(RGB(0x039855));

        private static readonly ColorGroup IconSuccessBorderColors = new(RGB(0x027948));

        private static readonly ColorGroup IconWarningColors = new(RGB(0xF79009));

        private static readonly ColorGroup IconWarningBorderColors = new(RGB(0xDC6803));

        private static readonly ColorGroup IconErrorColors = new(RGB(0xD92020));

        private static readonly ColorGroup IconErrorBorderColors = new(RGB(0xB41818));

        private static readonly ColorGroup IconStrokeColors = TitleTextColors;

        public ResolutionResources(int scaleTimes1M)
        {
            Scale = scaleTimes1M / 1048576.0;

            Main.Logger?.Log($"loading resources for scale {Scale}");

            Margin = Scaled(BaseMargin);

            Base = new GUIStyle
            {
                name = "Iridium Base",
                imagePosition = ImagePosition.ImageLeft,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Overflow,
                fontStyle = FontStyle.Normal,
                richText = true,
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0)
            };

            SetupTextSize(Base, ScaledInt(BaseTextSize));
            SetupTextColors(Base, NormalTextColors);

            Container = new GUIStyle(Base)
            {
                name = "Iridium Container"
            };

            var scaledPadding = ScaledInt(ContainerPadding);

            PaddingContainer = new GUIStyle(Base)
            {
                name = "Iridium Padding Container",
                padding = new RectOffset(scaledPadding, scaledPadding, scaledPadding, scaledPadding)
            };

            Background0Container = new GUIStyle(Base)
            {
                name = "Iridium Container With Background 0"
            };

            SetupRoundedRectangleBackground(Background0Container, Scaled(BackgroundRadius), Background0Colors);

            Background1Container = new GUIStyle(Base)
            {
                name = "Iridium Container With Background 1"
            };

            SetupRoundedRectangleBackground(Background1Container, Scaled(BackgroundRadius), Background1Colors);

            HorizontalSeparator = new GUIStyle(Base)
            {
                name = "Iridium Horizontal Separator",
                fixedHeight = 1
            };

            SetupRectangleBackground(HorizontalSeparator, SeparatorColors);

            VerticalSeparator = new GUIStyle(Base)
            {
                name = "Iridium Vertical Separator",
                fixedWidth = 1
            };

            SetupRectangleBackground(VerticalSeparator, SeparatorColors);

            NormalText = new GUIStyle(Base)
            {
                name = "Iridium Normal Text",
                alignment = TextAnchor.MiddleLeft
            };

            SetupTextSize(NormalText, ScaledInt(BaseTextSize), true);

            var subtitleMargin = (int)Scaled(SubtitleAdditionalMargin);

            SubtitleText = new GUIStyle(Base)
            {
                name = "Iridium Subtitle Text",
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, subtitleMargin, subtitleMargin)
            };

            SetupTextSize(SubtitleText, ScaledInt(SubtitleTextSize), true);
            SetupTextColors(SubtitleText, SubtitleTextColors);

            var titleMargin = (int)Scaled(TitleAdditionalMargin);

            TitleText = new GUIStyle(Base)
            {
                name = "Iridium Title Text",
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, titleMargin, titleMargin)
            };

            SetupTextSize(TitleText, ScaledInt(TitleTextSize), true);
            SetupTextColors(TitleText, TitleTextColors);

            SecondaryText = new GUIStyle(Base)
            {
                name = "Iridium Secondary Text",
                alignment = TextAnchor.MiddleLeft
            };

            SetupTextSize(SecondaryText, ScaledInt(SecondaryTextSize), true);
            SetupTextColors(SecondaryText, SecondaryTextColors);

            ElementButton = new GUIStyle(Base)
            {
                name = "Iridium Element Button"
            };

            SetupTextSize(ElementButton, ScaledInt(BaseTextSize), true);
            SetupTextColors(ElementButton, TitleTextColors);
            SetupRoundedRectangleBackground(ElementButton, Scaled(ButtonRadius), ElementColors);

            PrimaryButton = new GUIStyle(Base)
            {
                name = "Iridium Primary Button"
            };

            SetupTextSize(PrimaryButton, ScaledInt(BaseTextSize), true);
            SetupTextColors(PrimaryButton, TitleTextColors);
            SetupRoundedRectangleBackground(PrimaryButton, Scaled(ButtonRadius), PrimaryColors);

            var squareIconSize = ScaledInt(SquareIconSize);

            CheckboxOff = new GUIStyle(Base)
            {
                name = "Iridium Checkbox Off",
                fixedWidth = squareIconSize,
                fixedHeight = squareIconSize
            };

            SetupSquareIcon(CheckboxOff, CheckboxOffColors, CheckboxOffBorderColors, CheckboxCheckmarkColors, DrawNothing);

            CheckboxOn = new GUIStyle(Base)
            {
                name = "Iridium Checkbox On",
                fixedWidth = squareIconSize,
                fixedHeight = squareIconSize
            };

            SetupSquareIcon(CheckboxOn, CheckboxOnColors, CheckboxOnBorderColors, CheckboxCheckmarkColors, DrawCheckmark);

            ArrowButtonRight = new GUIStyle(Base)
            {
                name = "Iridium Arrow Button Right",
                fixedWidth = squareIconSize,
                fixedHeight = squareIconSize
            };

            SetupSquareIcon(ArrowButtonRight, ArrowButtonColors, ArrowButtonBorderColors, ArrowButtonArrowColors, DrawRightArrow);

            ArrowButtonDown = new GUIStyle(Base)
            {
                name = "Iridium Arrow Button Down",
                fixedWidth = squareIconSize,
                fixedHeight = squareIconSize
            };

            SetupSquareIcon(ArrowButtonDown, ArrowButtonColors, ArrowButtonBorderColors, ArrowButtonArrowColors, DrawDownArrow);

            ArrowButtonLeft = new GUIStyle(Base)
            {
                name = "Iridium Arrow Button Left",
                fixedWidth = squareIconSize,
                fixedHeight = squareIconSize
            };

            SetupSquareIcon(ArrowButtonLeft, ArrowButtonColors, ArrowButtonBorderColors, ArrowButtonArrowColors, DrawLeftArrow);

            ArrowButtonUp = new GUIStyle(Base)
            {
                name = "Iridium Arrow Button Up",
                fixedWidth = squareIconSize,
                fixedHeight = squareIconSize
            };

            SetupSquareIcon(ArrowButtonUp, ArrowButtonColors, ArrowButtonBorderColors, ArrowButtonArrowColors, DrawUpArrow);

            var switchWidth = ScaledInt(SwitchWidth);
            var switchHeight = ScaledInt(SwitchHeight);

            SwitchOff = new GUIStyle(Base)
            {
                name = "Iridium Switch Off",
                fixedWidth = switchWidth,
                fixedHeight = switchHeight
            };

            SetupSwitch(SwitchOff, false, SwitchOffColors, SwitchButtonColors);

            SwitchOn = new GUIStyle(Base)
            {
                name = "Iridium Switch On",
                fixedWidth = switchWidth,
                fixedHeight = switchHeight
            };

            SetupSwitch(SwitchOn, true, SwitchOnColors, SwitchButtonColors);

            TextField = new GUIStyle(Base)
            {
                name = "Iridium Text Field",
                alignment = TextAnchor.MiddleLeft
            };

            SetupTextSize(TextField, ScaledInt(BaseTextSize), true);
            SetupBorderedRoundedRectangleBackground(TextField, Scaled(TextFieldRadius), Scaled(TextFieldBorder), TextFieldColors, TextFieldBorderColors);

            var iconSize = ScaledInt(IconSize);

            IconInformation = new GUIStyle(Base)
            {
                name = "Iridium Icon Information",
                fixedWidth = iconSize,
                fixedHeight = iconSize
            };

            SetupIcon(IconInformation, IconInformationColors, IconInformationBorderColors, IconStrokeColors, DrawInformation);

            IconSuccess = new GUIStyle(Base)
            {
                name = "Iridium Icon Success",
                fixedWidth = iconSize,
                fixedHeight = iconSize
            };

            SetupIcon(IconSuccess, IconSuccessColors, IconSuccessBorderColors, IconStrokeColors, DrawSuccess);

            IconWarning = new GUIStyle(Base)
            {
                name = "Iridium Icon Warning",
                fixedWidth = iconSize,
                fixedHeight = iconSize
            };

            SetupIcon(IconWarning, IconWarningColors, IconWarningBorderColors, IconStrokeColors, DrawWarning);

            IconError = new GUIStyle(Base)
            {
                name = "Iridium Icon Error",
                fixedWidth = iconSize,
                fixedHeight = iconSize
            };

            SetupIcon(IconError, IconErrorColors, IconErrorBorderColors, IconStrokeColors, DrawError);
        }

        private double Scale { get; }

        public GUIStyle Base { get; }

        public GUIStyle Container { get; }

        public GUIStyle PaddingContainer { get; }

        public GUIStyle Background0Container { get; }

        public GUIStyle Background1Container { get; }

        public GUIStyle HorizontalSeparator { get; }

        public GUIStyle VerticalSeparator { get; }

        public GUIStyle NormalText { get; }

        public GUIStyle SubtitleText { get; }

        public GUIStyle TitleText { get; }

        public GUIStyle SecondaryText { get; }

        public GUIStyle ElementButton { get; }

        public GUIStyle PrimaryButton { get; }

        public GUIStyle CheckboxOff { get; }

        public GUIStyle CheckboxOn { get; }

        public GUIStyle ArrowButtonRight { get; }

        public GUIStyle ArrowButtonDown { get; }

        public GUIStyle ArrowButtonLeft { get; }

        public GUIStyle ArrowButtonUp { get; }

        public GUIStyle SwitchOff { get; }

        public GUIStyle SwitchOn { get; }

        public GUIStyle TextField { get; }

        public GUIStyle IconInformation { get; }

        public GUIStyle IconSuccess { get; }

        public GUIStyle IconWarning { get; }

        public GUIStyle IconError { get; }

        public double Margin { get; }

        public List<Texture2D> Textures { get; } = [];

        public void DestroyTextures()
        {
            foreach (var texture in Textures)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
            Textures.Clear();
        }

        public double Scaled(double value)
        {
            return Scale * value;
        }

        public int ScaledInt(double value)
        {
            return (int)(Scale * value);
        }

        private static Color RGB(long rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255F,
                ((rgb >> 8) & 0xFF) / 255F,
                (rgb & 0xFF) / 255F,
                1
            );
        }

        private static Color ARGB(long rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255F,
                ((rgb >> 8) & 0xFF) / 255F,
                (rgb & 0xFF) / 255F,
                ((rgb >> 24) & 0xFF) / 255F
            );
        }

        private Texture2D RenderImage(int width, int height, Action<Graphics> renderer)
        {
            Color[] colors;

            {
                byte[] byteData;

                {
                    using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    using var graphics = Graphics.FromImage(bitmap);
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.Clear(System.Drawing.Color.Transparent);
                    renderer(graphics);
                    var rect = new Rectangle(0, 0, width, height);
                    var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    var bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
                    byteData = new byte[bytes];
                    Marshal.Copy(bitmapData.Scan0, byteData, 0, bytes);
                    bitmap.UnlockBits(bitmapData);
                }

                colors = new Color[width * height];

                var d = height * width - width;

                for (var i = 0; i < colors.Length; i++)
                {
                    var baseAddress = (i % width * 2 - i + d) * 4;
                    colors[i] = new Color(
                        byteData[baseAddress + 2] / 255F,
                        byteData[baseAddress + 1] / 255F,
                        byteData[baseAddress] / 255F,
                        byteData[baseAddress + 3] / 255F
                    );
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.SetPixels(colors);
            texture.Apply();
            Textures.Add(texture);
            return texture;
        }

        private static void RoundedRectangle(
            GraphicsPath path,
            double x,
            double y,
            double width,
            double height,
            double radius
        )
        {
            radius = Math.Max(0.0, Math.Min(Math.Min(radius, width / 2.0), height / 2.0));
            var r = (float)radius;
            var r2 = r + r;
            var w = (float)width;
            var h = (float)height;
            var fx = (float)x;
            var fy = (float)y;
            path.AddArc(fx + w - r2, fy, r2, r2, 270, 90);
            path.AddArc(fx + w - r2, fy + h - r2, r2, r2, 0, 90);
            path.AddArc(fx, fy + h - r2, r2, r2, 90, 90);
            path.AddArc(fx, fy, r2, r2, 180, 90);
            path.CloseFigure();
        }

        private static void RoundedCorner(
            GraphicsPath path,
            double radius,
            double xA,
            double yA,
            double xC,
            double yC,
            double xB,
            double yB
        )
        {
            var x1 = xA - xC;
            var y1 = yA - yC;
            var x2 = xB - xC;
            var y2 = yB - yC;
            var d1 = Math.Sqrt(x1 * x1 + y1 * y1);
            var d2 = Math.Sqrt(x2 * x2 + y2 * y2);
            x1 /= d1;
            y1 /= d1;
            x2 /= d2;
            y2 /= d2;
            var d = x1 * x2 + y1 * y2;
            var a1 = (Math.Atan2(y1, x1) * 180 / Math.PI % 360 + 360) % 360;
            var a2 = (Math.Atan2(y2, x2) * 180 / Math.PI % 360 + 360) % 360;
            if (a1 > a2) (a1, a2) = (a2, a1);
            if (a2 - a1 > 180) (a1, a2) = (a2, a1 + 360);

            var m = radius / Math.Sqrt(1 - d * d);
            var x = xC + (x1 + x2) * m;
            var y = yC + (y1 + y2) * m;

            path.AddArc(
                (float)(x - radius),
                (float)(y - radius),
                (float)(radius + radius),
                (float)(radius + radius),
                (float)(a2 + 90),
                (float)(a1 - a2 + 180)
            );
        }

        private static double RoundedCornerOccupiedSpace(
            double radius,
            double xA,
            double yA,
            double xC,
            double yC,
            double xB,
            double yB
        )
        {
            var x1 = xA - xC;
            var y1 = yA - yC;
            var x2 = xB - xC;
            var y2 = yB - yC;
            var d1 = Math.Sqrt(x1 * x1 + y1 * y1);
            var d2 = Math.Sqrt(x2 * x2 + y2 * y2);
            x1 /= d1;
            y1 /= d1;
            x2 /= d2;
            y2 /= d2;
            var d = x1 * x2 + y1 * y2;
            var x = x1 + x2;
            var y = y1 + y2;
            return radius * Math.Sqrt((x * x + y * y) / (2 - d - d));
        }

        private static void LinesWithRoundedCorner(
            GraphicsPath path,
            double radius,
            bool closePath,
            params PointF[] points
        )
        {
            var count = points.Length;

            if (count <= 1) return;
            if (count == 2)
            {
                path.AddLine(points[0], points[1]);
                return;
            }

            radius = Math.Max(radius, 0);

            List<double> distances = [];

            for (var i = 0; i < count; i++)
            {
                var last = points[i];
                var curr = points[i + 1 == count ? 0 : i + 1];
                var dx = curr.X - last.X;
                var dy = curr.Y - last.Y;
                distances.Add(Math.Sqrt(dx * dx + dy * dy));
            }

            List<double> radii = [];

            for (var i = 1; i < count - 1; i++)
                radii.Add(Math.Min(radius, Math.Min(distances[i - 1] / 2, distances[i] / 2)));

            if (closePath)
            {
                radii.Add(Math.Min(radius, Math.Min(distances[^2] / 2, distances[^1] / 2)));
                radii.Add(Math.Min(radius, Math.Min(distances[^1] / 2, distances[0] / 2)));
            }
            else
            {
                radii[0] = Math.Min(radius, Math.Min(distances[0], distances[1] / 2));
                radii[^1] = Math.Min(
                    radius,
                    Math.Min(distances[^3] / 2, distances[^2])
                );
                var distance = distances[0];
                var p1 = points[0];
                var p2 = points[1];
                var p3 = points[2];
                var length = distance - RoundedCornerOccupiedSpace(radii[0], p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y);
                var x = (points[1].X - points[0].X) * length / distance;
                var y = (points[1].Y - points[0].Y) * length / distance;
                path.AddLine(points[0], points[0] + new SizeF((float)x, (float)y));
            }

            for (var i = 0; i < radii.Count; i++)
            {
                var cornerRadius = radii[i];
                var p1 = points[i];
                var p2 = points[i + 1 >= count ? i + 1 - count : i + 1];
                var p3 = points[i + 2 >= count ? i + 2 - count : i + 2];
                RoundedCorner(path, cornerRadius, p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y);
            }

            if (closePath) path.CloseFigure();
            else path.AddLine(path.GetLastPoint(), points[^1]);
        }

        private Texture2D RenderRectangleImage(
            int width,
            int height,
            Color color
        )
        {
            return RenderImage(width, height, graphics =>
            {
                using var brush = new SolidBrush(DrawingColor(color));
                graphics.FillRectangle(brush, 0, 0, width, height);
            });
        }

        private Texture2D RenderRoundedRectangleImage(
            int width,
            int height,
            double radius,
            Color color
        )
        {
            return RenderImage(width, height, graphics =>
            {
                using var path = new GraphicsPath();
                RoundedRectangle(path, 0, 0, width, height, radius);
                using var brush = new SolidBrush(DrawingColor(color));
                graphics.FillPath(brush, path);
            });
        }

        private Texture2D RenderBorderedRoundedRectangleImage(
            int width,
            int height,
            double radius,
            double border,
            Color color,
            Color borderColor
        )
        {
            return RenderImage(width, height, graphics =>
            {
                {
                    using var path = new GraphicsPath();
                    RoundedRectangle(path, 0, 0, width, height, radius);
                    using var brush = new SolidBrush(DrawingColor(borderColor));
                    graphics.FillPath(brush, path);
                }
                {
                    using var path = new GraphicsPath();
                    RoundedRectangle(
                        path,
                        border,
                        border,
                        width - border - border,
                        height - border - border,
                        radius - border
                    );
                    using var brush = new SolidBrush(DrawingColor(color));
                    graphics.FillPath(brush, path);
                }
            });
        }

        private Texture2D RenderSquareIconImage(
            Color color,
            Color borderColor,
            Color strokeColor,
            Action<Graphics, int, Color> renderer
        )
        {
            var size = ScaledInt(SquareIconSize);
            var radius = Scaled(SquareIconRadius);
            var border = Scaled(SquareIconBorder);
            return RenderImage(size, size, graphics =>
            {
                {
                    using var path = new GraphicsPath();
                    RoundedRectangle(path, 0, 0, size, size, radius);
                    using var brush = new SolidBrush(DrawingColor(borderColor));
                    graphics.FillPath(brush, path);
                }
                {
                    using var path = new GraphicsPath();
                    RoundedRectangle(
                        path,
                        border,
                        border,
                        size - border - border,
                        size - border - border,
                        radius - border
                    );
                    using var brush = new SolidBrush(DrawingColor(color));
                    graphics.FillPath(brush, path);
                }
                renderer(graphics, size, strokeColor);
            });
        }

        private Texture2D RenderSwitchImage(
            bool on,
            Color color,
            Color buttonColor
        )
        {
            var width = ScaledInt(SwitchWidth);
            var height = ScaledInt(SwitchHeight);
            var radius = height / 2F;
            var buttonRadius = (float)Scaled(SwitchButtonRadius);
            var buttonX = on ? width - radius - buttonRadius : radius - buttonRadius;
            var buttonY = radius - buttonRadius;
            return RenderImage(width, height, graphics =>
            {
                {
                    using var path = new GraphicsPath();
                    RoundedRectangle(path, 0, 0, width, height, radius);
                    using var brush = new SolidBrush(DrawingColor(color));
                    graphics.FillPath(brush, path);
                }
                {
                    using var path = new GraphicsPath();
                    path.AddArc(
                        buttonX,
                        buttonY,
                        buttonRadius + buttonRadius,
                        buttonRadius + buttonRadius,
                        0,
                        360
                    );
                    path.CloseFigure();
                    using var brush = new SolidBrush(DrawingColor(buttonColor));
                    graphics.FillPath(brush, path);
                }
            });
        }

        private Texture2D RenderIconImage(
            Color color,
            Color borderColor,
            Color strokeColor,
            Action<Graphics, int, Color> renderer
        )
        {
            var size = ScaledInt(IconSize);
            var border = (float)ScaledInt(IconBorder);
            return RenderImage(size, size, graphics =>
            {
                {
                    using var path = new GraphicsPath();
                    path.AddArc(
                        0,
                        0,
                        size,
                        size,
                        0,
                        360
                    );
                    path.CloseFigure();
                    using var brush = new SolidBrush(DrawingColor(borderColor));
                    graphics.FillPath(brush, path);
                }
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(
                        border,
                        border,
                        size - border - border,
                        size - border - border,
                        0,
                        360
                    );
                    path.CloseFigure();
                    using var brush = new SolidBrush(DrawingColor(color));
                    graphics.FillPath(brush, path);
                }
                renderer(graphics, size, strokeColor);
            });
        }

        private static void DrawNothing(Graphics graphics, int size, Color strokeColor)
        {
        }

        private static void DrawCheckmark(Graphics graphics, int size, Color strokeColor)
        {
            using var path = new GraphicsPath();
            path.AddLines([
                new PointF(size * 9 / 32F, size * 17 / 32F),
                new PointF(size * 13 / 32F, size * 21 / 32F),
                new PointF(size * 23 / 32F, size * 11 / 32F)
            ]);
            using var pen = new Pen(DrawingColor(strokeColor), size * 2 / 20F);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            graphics.DrawPath(pen, path);
        }

        private static void DrawArrow(Graphics graphics, int size, Color strokeColor, bool flip, bool rotate)
        {
            using var path = new GraphicsPath();
            path.AddLines([
                Transform(new PointF(size * 13 / 32F, size * 8 / 32F)),
                Transform(new PointF(size * 21 / 32F, size * 16 / 32F)),
                Transform(new PointF(size * 13 / 32F, size * 24 / 32F))
            ]);
            using var pen = new Pen(DrawingColor(strokeColor), size * 2 / 20F);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            graphics.DrawPath(pen, path);

            return;

            PointF Transform(PointF point)
            {
                var p = point;
                if (flip) p.X = size - p.X;
                if (rotate) p = new PointF(size - p.Y, p.X);
                return p;
            }
        }

        private static void DrawRightArrow(Graphics graphics, int size, Color strokeColor)
        {
            DrawArrow(graphics, size, strokeColor, false, false);
        }

        private static void DrawDownArrow(Graphics graphics, int size, Color strokeColor)
        {
            DrawArrow(graphics, size, strokeColor, false, true);
        }

        private static void DrawLeftArrow(Graphics graphics, int size, Color strokeColor)
        {
            DrawArrow(graphics, size, strokeColor, true, false);
        }

        private static void DrawUpArrow(Graphics graphics, int size, Color strokeColor)
        {
            DrawArrow(graphics, size, strokeColor, true, true);
        }

        private static void DrawInformation(Graphics graphics, int size, Color strokeColor)
        {
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(size * 4 / 20F, size * 4 / 20F, size * 12 / 20F, size * 12 / 20F, 0, 360);
                path.StartFigure();
                path.AddLine(size * 10 / 20F, size * 9.5F / 20F, size * 10 / 20F, size * 13 / 20F);
                using var pen = new Pen(DrawingColor(strokeColor), size * 1.5F / 20F);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                graphics.DrawPath(pen, path);
            }
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(size * 9.25F / 20F, size * 6.25F / 20F, size * 1.5F / 20F, size * 1.5F / 20F, 0, 360);
                path.CloseFigure();
                using var pen = new SolidBrush(DrawingColor(strokeColor));
                graphics.FillPath(pen, path);
            }
        }

        private static void DrawSuccess(Graphics graphics, int size, Color strokeColor)
        {
            var path = new GraphicsPath();
            path.AddArc(size * 4 / 20F, size * 4 / 20F, size * 12 / 20F, size * 12 / 20F, 0, 285);
            path.StartFigure();
            path.AddLines([
                new PointF(size * 8 / 20F, size * 9F / 20F),
                new PointF(size * 10 / 20F, size * 11F / 20F),
                new PointF(size * 16 / 20F, size * 5F / 20F)
            ]);
            using var pen = new Pen(DrawingColor(strokeColor), size * 1.5F / 20F);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            pen.LineJoin = LineJoin.Round;
            graphics.DrawPath(pen, path);
        }

        private static void DrawWarning(Graphics graphics, int size, Color strokeColor)
        {
            {
                var path = new GraphicsPath();
                LinesWithRoundedCorner(
                    path,
                    size * 2 / 20F,
                    true,
                    GetPoint(30, 9),
                    GetPoint(150, 9),
                    GetPoint(270, 9)
                );
                path.CloseFigure();
                path.AddLine(size * 10 / 20F, size * 7 / 20F, size * 10 / 20F, size * 10.5F / 20F);
                using var pen = new Pen(DrawingColor(strokeColor), size * 1.5F / 20F);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                graphics.DrawPath(pen, path);
            }
            {
                var path = new GraphicsPath();
                path.AddArc(size * 9.25F / 20F, size * 12.25F / 20F, size * 1.5F / 20F, size * 1.5F / 20F, 0, 360);
                path.CloseFigure();
                using var pen = new SolidBrush(DrawingColor(strokeColor));
                graphics.FillPath(pen, path);
            }

            return;

            PointF GetPoint(double angleDegrees, double distance)
            {
                return new PointF(
                    (float)(size * (10 + distance * Math.Cos(angleDegrees / 180 * Math.PI)) / 20),
                    (float)(size * (11 + distance * Math.Sin(angleDegrees / 180 * Math.PI)) / 20)
                );
            }
        }

        private static void DrawError(Graphics graphics, int size, Color strokeColor)
        {
            {
                var path = new GraphicsPath();
                path.AddArc(size * 4 / 20F, size * 4 / 20F, size * 12 / 20F, size * 12 / 20F, 0, 360);
                path.StartFigure();
                path.AddLine(size * 10 / 20F, size * 7 / 20F, size * 10 / 20F, size * 10.5F / 20F);
                using var pen = new Pen(DrawingColor(strokeColor), size * 1.5F / 20F);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                graphics.DrawPath(pen, path);
            }
            {
                var path = new GraphicsPath();
                path.AddArc(size * 9.25F / 20F, size * 12.25F / 20F, size * 1.5F / 20F, size * 1.5F / 20F, 0, 360);
                path.CloseFigure();
                using var pen = new SolidBrush(DrawingColor(strokeColor));
                graphics.FillPath(pen, path);
            }
        }

        private static void SetupTextSize(
            GUIStyle style,
            double size,
            bool isText = false
        )
        {
            style.fontSize = (int)size;
            style.contentOffset = isText
                ? new Vector2(0, -(float)(size * 0.1))
                : new Vector2(0, 0);
        }

        private static void SetupTextColors(
            GUIStyle style,
            ColorGroup textColors
        )
        {
            style.onNormal.textColor = style.normal.textColor = textColors.Normal;
            style.onHover.textColor = style.hover.textColor = textColors.Hovered;
            style.onActive.textColor = style.active.textColor = textColors.Active;
            style.onFocused.textColor = style.focused.textColor = textColors.Focused;
        }

        private void SetupRectangleBackground(
            GUIStyle style,
            ColorGroup colors
        )
        {
            const int size = 256;
            style.padding = style.border = new RectOffset(0, 0, 0, 0);
            style.onNormal.background = style.normal.background = RenderRectangleImage(size, size, colors.Normal);
            style.onHover.background = style.hover.background = RenderRectangleImage(size, size, colors.Hovered);
            style.onActive.background = style.active.background = RenderRectangleImage(size, size, colors.Active);
            style.onFocused.background = style.focused.background = RenderRectangleImage(size, size, colors.Focused);
        }

        private void SetupRoundedRectangleBackground(
            GUIStyle style,
            double radius,
            ColorGroup colors
        )
        {
            var borderSize = (int)Math.Ceiling(radius);
            var size = borderSize + 256;
            style.padding = style.border =
                new RectOffset(borderSize, borderSize, borderSize, borderSize);
            style.onNormal.background = style.normal.background = RenderRoundedRectangleImage(size, size, radius, colors.Normal);
            style.onHover.background = style.hover.background = RenderRoundedRectangleImage(size, size, radius, colors.Hovered);
            style.onActive.background = style.active.background = RenderRoundedRectangleImage(size, size, radius, colors.Active);
            style.onFocused.background = style.focused.background = RenderRoundedRectangleImage(size, size, radius, colors.Focused);
        }

        private void SetupBorderedRoundedRectangleBackground(
            GUIStyle style,
            double radius,
            double border,
            ColorGroup colors,
            ColorGroup borderColors
        )
        {
            var borderSize = (int)Math.Ceiling(radius);
            var size = borderSize + 256;
            style.padding = style.border =
                new RectOffset(borderSize, borderSize, borderSize, borderSize);
            style.onNormal.background = style.normal.background = RenderBorderedRoundedRectangleImage(size, size, radius, border, colors.Normal, borderColors.Normal);
            style.onHover.background = style.hover.background = RenderBorderedRoundedRectangleImage(size, size, radius, border, colors.Hovered, borderColors.Hovered);
            style.onActive.background = style.active.background = RenderBorderedRoundedRectangleImage(size, size, radius, border, colors.Active, borderColors.Active);
            style.onFocused.background = style.focused.background = RenderBorderedRoundedRectangleImage(size, size, radius, border, colors.Focused, borderColors.Focused);
        }

        private void SetupSquareIcon(
            GUIStyle style,
            ColorGroup colors,
            ColorGroup borderColors,
            ColorGroup strokeColors,
            Action<Graphics, int, Color> renderer
        )
        {
            style.onNormal.background = style.normal.background = RenderSquareIconImage(colors.Normal, borderColors.Normal, strokeColors.Normal, renderer);
            style.onHover.background = style.hover.background = RenderSquareIconImage(colors.Hovered, borderColors.Hovered, strokeColors.Hovered, renderer);
            style.onActive.background = style.active.background = RenderSquareIconImage(colors.Active, borderColors.Active, strokeColors.Active, renderer);
            style.onFocused.background = style.focused.background = RenderSquareIconImage(colors.Focused, borderColors.Focused, strokeColors.Focused, renderer);
        }

        private void SetupSwitch(
            GUIStyle style,
            bool on,
            ColorGroup colors,
            ColorGroup buttonColors
        )
        {
            style.onNormal.background = style.normal.background = RenderSwitchImage(on, colors.Normal, buttonColors.Normal);
            style.onHover.background = style.hover.background = RenderSwitchImage(on, colors.Hovered, buttonColors.Hovered);
            style.onActive.background = style.active.background = RenderSwitchImage(on, colors.Active, buttonColors.Active);
            style.onFocused.background = style.focused.background = RenderSwitchImage(on, colors.Focused, buttonColors.Focused);
        }

        private void SetupIcon(
            GUIStyle style,
            ColorGroup colors,
            ColorGroup borderColors,
            ColorGroup strokeColors,
            Action<Graphics, int, Color> renderer
        )
        {
            style.onNormal.background = style.normal.background = RenderIconImage(colors.Normal, borderColors.Normal, strokeColors.Normal, renderer);
            style.onHover.background = style.hover.background = RenderIconImage(colors.Hovered, borderColors.Hovered, strokeColors.Hovered, renderer);
            style.onActive.background = style.active.background = RenderIconImage(colors.Active, borderColors.Active, strokeColors.Active, renderer);
            style.onFocused.background = style.focused.background = RenderIconImage(colors.Focused, borderColors.Focused, strokeColors.Focused, renderer);
        }

        private static System.Drawing.Color DrawingColor(Color color)
        {
            return System.Drawing.Color.FromArgb(
                (int)(color.a * 255),
                (int)(color.r * 255),
                (int)(color.g * 255),
                (int)(color.b * 255)
            );
        }

        private class ColorGroup(Color normal, Color hovered, Color active, Color? focused = null)
        {
            public ColorGroup(Color color) : this(color, color, color, color)
            {
            }

            public Color Normal { get; } = normal;
            public Color Hovered { get; } = hovered;
            public Color Active { get; } = active;
            public Color Focused { get; } = focused ?? normal;
        }
    }
}