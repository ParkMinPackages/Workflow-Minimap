using System;

namespace ParkMinPackages.Workflow.Minimap.Interfaces
{
	public interface IAutoRunController : IDisposable
	{
		void AttachAutoRun();
		void DetachAutoRun();

		bool IsAutoRunning { get; }
	}
}
