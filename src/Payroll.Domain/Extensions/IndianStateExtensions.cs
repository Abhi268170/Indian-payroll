using Payroll.Domain.Enums;

namespace Payroll.Domain.Extensions;

public static class IndianStateExtensions
{
    public static string ToIsoCode(this IndianState state) => state switch
    {
        IndianState.AndhraPradesh => "AP",
        IndianState.ArunachalPradesh => "AR",
        IndianState.Assam => "AS",
        IndianState.Bihar => "BR",
        IndianState.Chhattisgarh => "CG",
        IndianState.Goa => "GA",
        IndianState.Gujarat => "GJ",
        IndianState.Haryana => "HR",
        IndianState.HimachalPradesh => "HP",
        IndianState.Jharkhand => "JH",
        IndianState.Karnataka => "KA",
        IndianState.Kerala => "KL",
        IndianState.MadhyaPradesh => "MP",
        IndianState.Maharashtra => "MH",
        IndianState.Manipur => "MN",
        IndianState.Meghalaya => "ML",
        IndianState.Mizoram => "MZ",
        IndianState.Nagaland => "NL",
        IndianState.Odisha => "OD",
        IndianState.Punjab => "PB",
        IndianState.Rajasthan => "RJ",
        IndianState.Sikkim => "SK",
        IndianState.TamilNadu => "TN",
        IndianState.Telangana => "TS",
        IndianState.Tripura => "TR",
        IndianState.UttarPradesh => "UP",
        IndianState.Uttarakhand => "UK",
        IndianState.WestBengal => "WB",
        IndianState.AndamanAndNicobar => "AN",
        IndianState.Chandigarh => "CH",
        IndianState.DadraAndNagarHaveliAndDamanAndDiu => "DN",
        IndianState.Delhi => "DL",
        IndianState.JammuAndKashmir => "JK",
        IndianState.Ladakh => "LA",
        IndianState.Lakshadweep => "LD",
        IndianState.Puducherry => "PY",
        _ => "MH"
    };
}
