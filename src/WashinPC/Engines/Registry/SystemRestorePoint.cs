using System;
using System.Management;

namespace WashinPC.Engines.Registry
{
    internal static class SystemRestorePoint
    {
        private const uint BeginSystemChange = 100;
        private const uint ApplicationInstall = 0;

        public static bool TryCreate(string description, out string error)
        {
            error = string.Empty;

            try
            {
                using (var cls = new ManagementClass(@"\\.\root\default:SystemRestore"))
                using (var inParams = cls.GetMethodParameters("CreateRestorePoint"))
                {
                    inParams["Description"] = string.IsNullOrWhiteSpace(description)
                        ? "WashinPC registry cleanup"
                        : description;
                    inParams["RestorePointType"] = ApplicationInstall;
                    inParams["EventType"] = BeginSystemChange;

                    using (var outParams = cls.InvokeMethod("CreateRestorePoint", inParams, null))
                    {
                        if (outParams == null)
                        {
                            error = "No response from SystemRestore service.";
                            return false;
                        }

                        var ret = Convert.ToInt32(outParams["ReturnValue"]);
                        if (ret == 0)
                            return true;

                        error = "CreateRestorePoint failed with code " + ret + ".";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
