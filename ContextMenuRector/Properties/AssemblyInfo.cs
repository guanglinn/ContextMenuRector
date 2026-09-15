using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: AssemblyProduct("ContextMenuRector")]
[assembly: AssemblyTitle("Windows Context Menu Manager")]
[assembly: AssemblyDescription("A free context menu manager for Windows.")]
[assembly: AssemblyCompany("Li Guanglin")]
[assembly: AssemblyCopyright("Copyright © 2020-2025 蓝点lilac; 2026-2027 Li Guanglin. All Rights Reserved.")]
[assembly: ComVisible(false)]
[assembly: Guid("35190ec1-2515-488d-a2e9-825d6ff67aa2")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
// 项目设置了 GenerateAssemblyInfo=false，SDK 不会再自动生成这两个平台特性。
// 缺少它们时，CA1416 会把整个程序集视为“可在所有平台上访问”，
// 从而对每一个 Windows 专有 API（如 Application.EnableVisualStyles）报警告。
[assembly: TargetPlatform("Windows8.0")]
[assembly: SupportedOSPlatform("Windows8.0")]
