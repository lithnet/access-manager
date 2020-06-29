using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct DsopUplevelFitlerFlags
	{
		/// <summary>
		/// Filter flags to use for an up-level scope, regardless of whether it is a mixed or native mode domain. This member can be a combination of one or more of the following flags.
		/// </summary>
		public DsopObjectFilterFlags BothModeFilter;

		/// <summary>
		/// Filter flags to use for an up-level domain in mixed mode. Mixed mode refers to an up-level domain that may have Windows NT 4.0 Backup Domain Controllers present. This member can be a combination of the flags listed in the flBothModes flags. The DSOP_FILTER_UNIVERSAL_GROUPS_SE flag has no affect in a mixed-mode domain because universal security groups do not exist in mixed mode domains.
		/// </summary>
		public DsopObjectFilterFlags MixedModeFilter;

		/// <summary>
		/// Filter flags to use for an up-level domain in native mode. Native mode refers to an up-level domain in which an administrator has enabled native mode operation. This member can be a combination of the flags listed in the flBothModes flags.
		/// </summary>
		public DsopObjectFilterFlags NativeModeFilter;
	}
}
