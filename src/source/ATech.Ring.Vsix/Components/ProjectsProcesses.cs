using System;
using System.Collections.Generic;

namespace ATech.Ring.Vsix.Components
{
    public class ProjectsProcesses
    {
        private readonly Action<string> _writeLog;
        private readonly Dictionary<Guid, string> _procToProj = new Dictionary<Guid, string>();
        private readonly Stack<string> _projNames = new Stack<string>();

        public ProjectsProcesses(Action<string> writeLog) => _writeLog = writeLog;

        public void AddProcessGuid(Guid processId)
        {
            var projName = _projNames.Pop();
            _procToProj.Add(processId, projName);
            _writeLog($"Started process {processId} bound to project {projName}");
        }

        public void AddProject(string projectName) => _projNames.Push(projectName);

        public string RemoveProjectByProcessId(Guid processId)
        {
            var projectName = _procToProj[processId];
            _procToProj.Remove(processId);
            _writeLog($"Terminated process {processId} bound to project {projectName}");
            return projectName;
        }
    }
}