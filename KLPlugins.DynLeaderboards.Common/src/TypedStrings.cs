using System;
using System.ComponentModel;

namespace KLPlugins.DynLeaderboards.Common;

[TypeConverter(typeof(CarClassTypeConverter))]
public readonly record struct CarClass : IComparable<CarClass> {
    private readonly string _cls;

    public CarClass(string cls) {
        this._cls = cls;
    }

    public static CarClass Default = new("None");

    public static CarClass? TryNew(string? cls) {
        if (cls == null) {
            return null;
        }

        return new CarClass(cls);
    }

    public string AsString() {
        return this._cls;
    }

    public override string ToString() {
        return this._cls;
    }

    public int CompareTo(CarClass other) {
        return string.Compare(this._cls, other._cls, StringComparison.Ordinal);
    }
}

internal sealed class CarClassTypeConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object? value
    ) {
        if (value is string val) {
            return new CarClass(val);
        }

        throw new NotSupportedException($"cannot convert from `{value?.GetType()}` to `CarClass`");
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
        return destinationType == typeof(string);
    }

    public override object ConvertTo(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object value,
        Type destinationType
    ) {
        if (value is CarClass cls && destinationType == typeof(string)) {
            return cls.AsString();
        }

        throw new NotSupportedException($"cannot convert from `CarClass` to `{destinationType}`");
    }
}

[TypeConverter(typeof(TeamCupCategoryTypeConverter))]
public readonly record struct TeamCupCategory : IComparable<TeamCupCategory> {
    private readonly string _cls;

    public TeamCupCategory(string cls) {
        this._cls = cls;
    }

    public static TeamCupCategory Default = new("Overall");

    public static TeamCupCategory? TryNew(string? cls) {
        if (cls == null) {
            return null;
        }

        return new TeamCupCategory(cls);
    }

    public string AsString() {
        return this._cls;
    }

    public override string ToString() {
        return this._cls;
    }

    public int CompareTo(TeamCupCategory other) {
        return string.Compare(this._cls, other._cls, StringComparison.Ordinal);
    }
}

internal sealed class TeamCupCategoryTypeConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object? value
    ) {
        if (value is string val) {
            return new TeamCupCategory(val);
        }

        throw new NotSupportedException($"cannot convert from `{value?.GetType()}` to `TeamCupCategory`");
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
        return destinationType == typeof(string);
    }

    public override object ConvertTo(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object value,
        Type destinationType
    ) {
        if (value is TeamCupCategory category && destinationType == typeof(string)) {
            return category.AsString();
        }

        throw new NotSupportedException($"cannot convert from `TeamCupCategory` to `{destinationType}`");
    }
}

[TypeConverter(typeof(DriverCategoryTypeConverter))]
public readonly record struct DriverCategory : IComparable<DriverCategory> {
    private readonly string _cls;

    public DriverCategory(string cls) {
        this._cls = cls;
    }

    public static DriverCategory Default = new("Platinum");

    public static DriverCategory? TryNew(string? cls) {
        if (cls == null) {
            return null;
        }

        return new DriverCategory(cls);
    }

    public string AsString() {
        return this._cls;
    }

    public override string ToString() {
        return this._cls;
    }

    public int CompareTo(DriverCategory other) {
        return string.Compare(this._cls, other._cls, StringComparison.Ordinal);
    }
}

internal sealed class DriverCategoryTypeConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object? value
    ) {
        if (value is string val) {
            return new DriverCategory(val);
        }

        throw new NotSupportedException($"cannot convert from `{value?.GetType()}` to `DriverCategory`");
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
        return destinationType == typeof(string);
    }

    public override object ConvertTo(
        ITypeDescriptorContext context,
        System.Globalization.CultureInfo culture,
        object value,
        Type destinationType
    ) {
        if (value is DriverCategory category && destinationType == typeof(string)) {
            return category.AsString();
        }

        throw new NotSupportedException($"cannot convert from `DriverCategory` to `{destinationType}`");
    }
}