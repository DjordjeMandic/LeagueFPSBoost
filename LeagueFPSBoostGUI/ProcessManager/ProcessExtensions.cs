using LeagueFPSBoost.Text;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LeagueFPSBoost.ProcessManagement
{
    public static class ProcessExtensions
    {
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static bool ProcessIDExists(this int id)
        {
            return Process.GetProcesses().Any(x => x.Id == id); 
        }

        public static bool ProcessIDExists(this object id)
        {
            try
            {
                var idInt = Convert.ToInt32(id);
                return idInt.ProcessIDExists();
            }
            catch (Exception ex)
            {
                logger.Warn(ex, Strings.exceptionThrown + $" while converting object id ({id}) to Int32." + Environment.NewLine);
                throw ex;
            }
        }

        public static Process GetProcessById(this int id)
        {
            if (id.ProcessIDExists())
            {
                return Process.GetProcessById(id);
            }
            var ex = new ArgumentException($"Process with an Id of {id} is not running.");
            logger.Warn(ex, Strings.exceptionThrown + $" while getting process by id ({id})." + Environment.NewLine);
            throw ex;
        }

        public static Process GetProcessById(this object id)
        {
            try
            {
                var idInt = Convert.ToInt32(id);
                return idInt.GetProcessById();
            }
            catch (Exception ex)
            {
                logger.Warn(ex, Strings.exceptionThrown + $" while converting object id ({id}) to Int32." + Environment.NewLine);
                throw ex;
            }
        }

        public static string GetProcessInfoForLogging(this Process process, bool printModules)
        {
            var piSB = new StringBuilder();
            piSB.AppendLine(Strings.tabWithLine + $"Process id: {process.Id}");
            piSB.AppendLine(Strings.doubleTabWithLine + $"Name: {process.ProcessName}");
            piSB.AppendLine(Strings.doubleTabWithLine + $"Base priority: {process.BasePriority}");
            piSB.AppendLine(Strings.doubleTabWithLine + $"Priority class: {process.PriorityClass}");
            piSB.AppendLine(Strings.doubleTabWithLine + $"Start time: {process.StartTime.ToString(Strings.startTimeFormat)}");
            piSB.AppendLine(Strings.doubleTabWithLine + $"Main module:");
            piSB.AppendLine(Strings.tripleTabWithLine + $"Name: {process.MainModule.ModuleName}");
            piSB.AppendLine(Strings.tripleTabWithLine + $"Path: {process.MainModule.FileName}");

            if (printModules)
            {
                piSB.AppendLine(Strings.doubleTabWithLine + "Modules:");
                foreach (ProcessModule pm in process.Modules)
                {
                    piSB.AppendLine(Strings.tab + Strings.doubleTabWithLine + pm.FileName);
                }
            }
            if (piSB[piSB.Length - 1] == '\n') piSB.Remove(piSB.Length - 1, 1);
            return piSB.ToString();
        }
    }
}
