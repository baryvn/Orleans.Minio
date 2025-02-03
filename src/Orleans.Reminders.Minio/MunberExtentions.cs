using System.Globalization;

namespace Orleans.Reminders.Minio
{
    public static class MunberExtentions
    {
        public static int ToInt(this string str)
        {
            try
            {
                int a;
                if (int.TryParse(str, out a))
                {
                    return a;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        public static int? ToIntNull(this string str)
        {
            try
            {
                int a;
                if (int.TryParse(str, out a))
                {
                    return a;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static long ToLong(this string str)
        {
            try
            {
                long a;
                if (long.TryParse(str, out a))
                {
                    return a;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        public static long? ToLongNull(this string str)
        {
            try
            {
                long a;
                if (long.TryParse(str, out a))
                {
                    return a;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }


        public static float ToFloat(this string str)
        {
            try
            {
                float a;
                if (float.TryParse(str, out a))
                {
                    return a;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        public static float? ToFloatNull(this string str)
        {
            try
            {
                float a;
                if (float.TryParse(str, out a))
                {
                    return a;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static double ToDouble(this string str)
        {
            try
            {
                double a;
                if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out a))
                {
                    return a;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        public static double? ToDoubleNull(this string str)
        {
            try
            {
                double a;
                if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out a))
                {
                    return a;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static decimal ToDecimal(this string str)
        {
            try
            {
                decimal a;
                if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out a))
                {
                    return a;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        public static decimal? ToDecimalNull(this string str)
        {
            try
            {
                decimal a;
                if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out a))
                {
                    return a;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string? ToStringNumber(this object obj, CultureInfo cultureInfo)
        {
            try
            {
                double a;
                if (double.TryParse(obj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out a))
                {
                    return a.ToString("###,###,###,###.#####", cultureInfo);
                }
                
                return obj != null? obj.ToString() : string.Empty;

            }
            catch
            {
                return "";
            }
        }
    }
}
