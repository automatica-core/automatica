using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Driver.DPT.Base;
using P3.Knx.Core.Driver.DPT.Dpt5;
using P3.Knx.Core.Driver.DPT.Dpt6;
using P3.Knx.Core.Driver.DPT.Dpt7;
using P3.Knx.Core.Driver.DPT.Dpt8;
using P3.Knx.Core.Driver.DPT.Dpt9;

namespace P3.Knx.Core.Driver.DPT
{
    public sealed class DptTranslator
    {
        public static readonly DptTranslator Instance = new DptTranslator();
        private readonly IDictionary<string, Dpt> _dataPoints = new Dictionary<string, Dpt>();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static DptTranslator()
        {
        }

        private DptTranslator()
        {
            List<Type> types = new List<Type> {
                typeof(Dpt1Translator),
                typeof(Dpt2Translator),
                typeof(Dpt3Translator),
                typeof(Dpt4Translator),
                typeof(Dpt5Translator),
                typeof(Dpt6020TypeTranslator),
                typeof(Dpt6Translator),
                typeof(Dpt9Translator),
                typeof(Dpt10Translator),
                typeof(Dpt11Translator),
                typeof(Dpt13Translator),
                typeof(Dpt16_000_Translator),
                typeof(Dpt16_001_Translator)
            };

            types.AddRange(Dpt9TypeTranslators.Types);
            types.AddRange(Dpt8TypeTranslators.Types);
            types.AddRange(Dpt7TypeTranslators.Types);
            types.AddRange(Dpt5TypeTranslators.Types);

            foreach (var t in types)
            {
                var dp = (Dpt)Activator.CreateInstance(t);

                foreach (string id in dp.Ids)
                {
                    _dataPoints.Add(id, dp);
                }
            }
        }

        public object FromDataPoint(string type, ReadOnlyMemory<byte> data)
        {
            try
            {
                if (data.Length == 0)
                {
                    throw new FromDataPointException("data cannot be null");
                }
                if (_dataPoints.TryGetValue(type, out var dpt))
                    return dpt.FromDataPoint(data.ToArray());

                if (_dataPoints.TryGetValue(type[0] + ".*", out var dpt2))
                    return dpt2.FromDataPoint(data.ToArray());
            }
            catch (FromDataPointException)
            {
                throw;
            }
            catch (NullReferenceException e)
            {
                throw new FromDataPointException(e);
            }
            catch
            {
                // ignore and return null
            }

            return null;
        }

        public byte[] ToDataPoint(string type, object value)
        {
            
            try
            {
                if (value == null)
                {
                    throw new ToDataPointException("value cannot be bull");
                }

                if (_dataPoints.TryGetValue(type, out var dpt))
                {
                    var ret  = dpt.ToDataPoint(value);

                    KnxHelper.Logger.LogDebug($"Converting dpt {type} value {value} to {Automatica.Core.Driver.Utility.Utils.ByteArrayToString(in ret)}");

                    return ret;
                }

                if (_dataPoints.TryGetValue(type[0] + ".*", out var dpt2))
                {
                    var ret = dpt2.ToDataPoint(value);
                    KnxHelper.Logger.LogDebug($"Converting dpt {type} value {value} to {Automatica.Core.Driver.Utility.Utils.ByteArrayToString(in ret)}");
                    return ret;
                }
            }
            catch (ToDataPointException)
            {
                throw;
            }
            catch (NullReferenceException e)
            {
                throw new ToDataPointException(e);
            }
            catch
            {
                // ignore and return null
            }

            return Array.Empty<byte>();
        }
    }
}
