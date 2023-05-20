<?xml version='1.0' encoding='UTF-8'?>
<Project Type="Project" LVVersion="20008000">
	<Item Name="My Computer" Type="My Computer">
		<Property Name="server.app.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.control.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="server.tcp.enabled" Type="Bool">false</Property>
		<Property Name="server.tcp.port" Type="Int">0</Property>
		<Property Name="server.tcp.serviceName" Type="Str">My Computer/VI Server</Property>
		<Property Name="server.tcp.serviceName.default" Type="Str">My Computer/VI Server</Property>
		<Property Name="server.vi.callsEnabled" Type="Bool">true</Property>
		<Property Name="server.vi.propertiesEnabled" Type="Bool">true</Property>
		<Property Name="specify.custom.address" Type="Bool">false</Property>
		<Item Name="LV Source" Type="Folder" URL="../LV Source">
			<Property Name="NI.DISK" Type="Bool">true</Property>
		</Item>
		<Item Name="Dependencies" Type="Dependencies">
			<Item Name="vi.lib" Type="Folder">
				<Item Name="8.6CompatibleGlobalVar.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/config.llb/8.6CompatibleGlobalVar.vi"/>
				<Item Name="Check if File or Folder Exists.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/Check if File or Folder Exists.vi"/>
				<Item Name="Clear Errors.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Clear Errors.vi"/>
				<Item Name="Error Cluster From Error Code.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Error Cluster From Error Code.vi"/>
				<Item Name="Get System Directory.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/sysdir.llb/Get System Directory.vi"/>
				<Item Name="LabVIEWHTTPClient.lvlib" Type="Library" URL="/&lt;vilib&gt;/httpClient/LabVIEWHTTPClient.lvlib"/>
				<Item Name="NI_FileType.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/lvfile.llb/NI_FileType.lvlib"/>
				<Item Name="NI_LVConfig.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/config.llb/NI_LVConfig.lvlib"/>
				<Item Name="NI_PackedLibraryUtility.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/LVLibp/NI_PackedLibraryUtility.lvlib"/>
				<Item Name="Path To Command Line String.vi" Type="VI" URL="/&lt;vilib&gt;/AdvancedString/Path To Command Line String.vi"/>
				<Item Name="PathToUNIXPathString.vi" Type="VI" URL="/&lt;vilib&gt;/Platform/CFURL.llb/PathToUNIXPathString.vi"/>
				<Item Name="Space Constant.vi" Type="VI" URL="/&lt;vilib&gt;/dlg_ctls.llb/Space Constant.vi"/>
				<Item Name="System Directory Type.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/sysdir.llb/System Directory Type.ctl"/>
				<Item Name="Trim Whitespace.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Trim Whitespace.vi"/>
				<Item Name="Version To Dotted String.vi" Type="VI" URL="/&lt;vilib&gt;/_xctls/Version To Dotted String.vi"/>
				<Item Name="whitespace.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/whitespace.ctl"/>
				<Item Name="XControlSupport.lvlib" Type="Library" URL="/&lt;vilib&gt;/_xctls/XControlSupport.lvlib"/>
			</Item>
		</Item>
		<Item Name="Build Specifications" Type="Build">
			<Item Name="OPP.Mixer" Type=".NET Interop Assembly">
				<Property Name="App_copyErrors" Type="Bool">true</Property>
				<Property Name="App_INI_aliasGUID" Type="Str">{8B41EFDD-A399-4FB9-94D3-33607553A938}</Property>
				<Property Name="App_INI_GUID" Type="Str">{A8C22E0B-A264-4420-90A0-AC623251E864}</Property>
				<Property Name="App_serverConfig.httpPort" Type="Int">8002</Property>
				<Property Name="App_serverType" Type="Int">0</Property>
				<Property Name="Bld_autoIncrement" Type="Bool">true</Property>
				<Property Name="Bld_buildCacheID" Type="Str">{806439BA-CE8C-4F89-8CA4-6D96269DE37C}</Property>
				<Property Name="Bld_buildSpecName" Type="Str">OPP.Mixer</Property>
				<Property Name="Bld_excludeInlineSubVIs" Type="Bool">true</Property>
				<Property Name="Bld_excludeLibraryItems" Type="Bool">true</Property>
				<Property Name="Bld_excludePolymorphicVIs" Type="Bool">true</Property>
				<Property Name="Bld_localDestDir" Type="Path">../Build</Property>
				<Property Name="Bld_localDestDirType" Type="Str">relativeToProject</Property>
				<Property Name="Bld_modifyLibraryFile" Type="Bool">true</Property>
				<Property Name="Bld_previewCacheID" Type="Str">{976254DA-0CF7-439F-80D3-98B2FB702728}</Property>
				<Property Name="Bld_version.build" Type="Int">22</Property>
				<Property Name="Bld_version.major" Type="Int">1</Property>
				<Property Name="Destination[0].destName" Type="Str">OPP.Mixer.dll</Property>
				<Property Name="Destination[0].path" Type="Path">../Build/NI_AB_PROJECTNAME.dll</Property>
				<Property Name="Destination[0].path.type" Type="Str">relativeToProject</Property>
				<Property Name="Destination[0].preserveHierarchy" Type="Bool">true</Property>
				<Property Name="Destination[0].type" Type="Str">App</Property>
				<Property Name="Destination[1].destName" Type="Str">Support Directory</Property>
				<Property Name="Destination[1].path" Type="Path">../Build</Property>
				<Property Name="Destination[1].path.type" Type="Str">relativeToProject</Property>
				<Property Name="DestinationCount" Type="Int">2</Property>
				<Property Name="DotNET2011CompatibilityMode" Type="Bool">false</Property>
				<Property Name="DotNETAssembly_ClassName" Type="Str">Mixer</Property>
				<Property Name="DotNETAssembly_delayOSMsg" Type="Bool">true</Property>
				<Property Name="DotNETAssembly_Namespace" Type="Str">OPP</Property>
				<Property Name="DotNETAssembly_signAssembly" Type="Bool">false</Property>
				<Property Name="DotNETAssembly_StrongNameKeyFileItemID" Type="Ref"></Property>
				<Property Name="DotNETAssembly_StrongNameKeyGUID" Type="Str">{3DC61CDE-18D5-4B50-B408-B78A4117BD13}</Property>
				<Property Name="Source[0].itemID" Type="Str">{3AA8AE16-28D0-4496-B16D-F4B2282B56DF}</Property>
				<Property Name="Source[0].type" Type="Str">Container</Property>
				<Property Name="Source[1].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">Close</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">Mixer.Close.vi</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">success</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[1].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.Close.vi</Property>
				<Property Name="Source[1].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[1].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[2].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">Open</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">Mixer.Open.vi</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">success</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[2].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.Open.vi</Property>
				<Property Name="Source[2].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[2].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[3].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">Initialize</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">Mixer.Initialize.vi</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">baseURL</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[3].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.Initialize.vi</Property>
				<Property Name="Source[3].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[3].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[4].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">0</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">address</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">2</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">message</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">2</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]MethodName" Type="Str">IsAvailable</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]VIName" Type="Str">Mixer.IsAvailable.vi</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">available</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">4</Property>
				<Property Name="Source[4].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.IsAvailable.vi</Property>
				<Property Name="Source[4].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[4].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[5].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">channel</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">5</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">5</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">property</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]MethodName" Type="Str">SetChannelProperty</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]VIName" Type="Str">Mixer.Set Channel Property.vi</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">value</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">4</Property>
				<Property Name="Source[5].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.Set Channel Property.vi</Property>
				<Property Name="Source[5].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[5].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[6].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">ConnectMonitor</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">Mixer.Connect Monitor.vi</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">I32</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">source</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[6].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.Connect Monitor.vi</Property>
				<Property Name="Source[6].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[6].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[7].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">MuteAudioStream</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">Mixer.MuteAudioStream.vi</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">value</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[7].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.MuteAudioStream.vi</Property>
				<Property Name="Source[7].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[7].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[8].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]MethodName" Type="Str">ToggleTalkback</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIName" Type="Str">Mixer.ToggleTalkback.vi</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">1</Property>
				<Property Name="Source[8].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/Mixer.ToggleTalkback.vi</Property>
				<Property Name="Source[8].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[8].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="SourceCount" Type="Int">9</Property>
				<Property Name="TgtF_companyName" Type="Str">Mass General Brigham</Property>
				<Property Name="TgtF_fileDescription" Type="Str">OPP.Mixer</Property>
				<Property Name="TgtF_internalName" Type="Str">OPP.Mixer</Property>
				<Property Name="TgtF_legalCopyright" Type="Str">Copyright © 2023 Mass General Brigham</Property>
				<Property Name="TgtF_productName" Type="Str">OPP.Mixer</Property>
				<Property Name="TgtF_targetfileGUID" Type="Str">{D8692FB9-86A2-4718-812C-DA4BA0E3834A}</Property>
				<Property Name="TgtF_targetfileName" Type="Str">OPP.Mixer.dll</Property>
				<Property Name="TgtF_versionIndependent" Type="Bool">true</Property>
			</Item>
		</Item>
	</Item>
</Project>
