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
				<Item Name="_2DArrToArrWfms.vi" Type="VI" URL="/&lt;vilib&gt;/sound2/lvsound2.llb/_2DArrToArrWfms.vi"/>
				<Item Name="Application Directory.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/file.llb/Application Directory.vi"/>
				<Item Name="BuildHelpPath.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/BuildHelpPath.vi"/>
				<Item Name="Check if File or Folder Exists.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/libraryn.llb/Check if File or Folder Exists.vi"/>
				<Item Name="Check Special Tags.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Check Special Tags.vi"/>
				<Item Name="Clear Errors.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Clear Errors.vi"/>
				<Item Name="Convert property node font to graphics font.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Convert property node font to graphics font.vi"/>
				<Item Name="Details Display Dialog.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Details Display Dialog.vi"/>
				<Item Name="DialogType.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/DialogType.ctl"/>
				<Item Name="DialogTypeEnum.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/DialogTypeEnum.ctl"/>
				<Item Name="Error Cluster From Error Code.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Error Cluster From Error Code.vi"/>
				<Item Name="Error Code Database.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Error Code Database.vi"/>
				<Item Name="ErrWarn.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/ErrWarn.ctl"/>
				<Item Name="eventvkey.ctl" Type="VI" URL="/&lt;vilib&gt;/event_ctls.llb/eventvkey.ctl"/>
				<Item Name="Find Tag.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Find Tag.vi"/>
				<Item Name="Format Message String.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Format Message String.vi"/>
				<Item Name="G-Audio.lvlib" Type="Library" URL="/&lt;vilib&gt;/Dataflow_G/G-Audio/G-Audio.lvlib"/>
				<Item Name="g_audio_64.dll" Type="Document" URL="/&lt;vilib&gt;/Dataflow_G/G-Audio/lib/g_audio_64.dll"/>
				<Item Name="General Error Handler Core CORE.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/General Error Handler Core CORE.vi"/>
				<Item Name="General Error Handler.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/General Error Handler.vi"/>
				<Item Name="Get String Text Bounds.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Get String Text Bounds.vi"/>
				<Item Name="Get System Directory.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/sysdir.llb/Get System Directory.vi"/>
				<Item Name="Get Text Rect.vi" Type="VI" URL="/&lt;vilib&gt;/picture/picture.llb/Get Text Rect.vi"/>
				<Item Name="GetHelpDir.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/GetHelpDir.vi"/>
				<Item Name="GetRTHostConnectedProp.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/GetRTHostConnectedProp.vi"/>
				<Item Name="Longest Line Length in Pixels.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Longest Line Length in Pixels.vi"/>
				<Item Name="LVBoundsTypeDef.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/miscctls.llb/LVBoundsTypeDef.ctl"/>
				<Item Name="LVNumericRepresentation.ctl" Type="VI" URL="/&lt;vilib&gt;/numeric/LVNumericRepresentation.ctl"/>
				<Item Name="LVPoint32TypeDef.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/miscctls.llb/LVPoint32TypeDef.ctl"/>
				<Item Name="LVPositionTypeDef.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/miscctls.llb/LVPositionTypeDef.ctl"/>
				<Item Name="LVRectTypeDef.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/miscctls.llb/LVRectTypeDef.ctl"/>
				<Item Name="NI_AALBase.lvlib" Type="Library" URL="/&lt;vilib&gt;/Analysis/NI_AALBase.lvlib"/>
				<Item Name="NI_Data Type.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/Data Type/NI_Data Type.lvlib"/>
				<Item Name="NI_FileType.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/lvfile.llb/NI_FileType.lvlib"/>
				<Item Name="NI_LVConfig.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/config.llb/NI_LVConfig.lvlib"/>
				<Item Name="NI_PackedLibraryUtility.lvlib" Type="Library" URL="/&lt;vilib&gt;/Utility/LVLibp/NI_PackedLibraryUtility.lvlib"/>
				<Item Name="Not Found Dialog.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Not Found Dialog.vi"/>
				<Item Name="Search and Replace Pattern.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Search and Replace Pattern.vi"/>
				<Item Name="Set Bold Text.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Set Bold Text.vi"/>
				<Item Name="Set String Value.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Set String Value.vi"/>
				<Item Name="Space Constant.vi" Type="VI" URL="/&lt;vilib&gt;/dlg_ctls.llb/Space Constant.vi"/>
				<Item Name="System Directory Type.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/sysdir.llb/System Directory Type.ctl"/>
				<Item Name="TagReturnType.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/TagReturnType.ctl"/>
				<Item Name="Three Button Dialog CORE.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Three Button Dialog CORE.vi"/>
				<Item Name="Three Button Dialog.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Three Button Dialog.vi"/>
				<Item Name="Trim Whitespace.vi" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/Trim Whitespace.vi"/>
				<Item Name="whitespace.ctl" Type="VI" URL="/&lt;vilib&gt;/Utility/error.llb/whitespace.ctl"/>
			</Item>
			<Item Name="_ChannelSupport.lvlib" Type="Library" URL="/&lt;resource&gt;/ChannelSupport/_ChannelSupport/_ChannelSupport.lvlib"/>
			<Item Name="ChannelProbePositionAndTitle.vi" Type="VI" URL="/&lt;resource&gt;/ChannelSupport/_ChannelSupport/ChannelProbePositionAndTitle.vi"/>
			<Item Name="ChannelProbeWindowStagger.vi" Type="VI" URL="/&lt;resource&gt;/ChannelSupport/_ChannelSupport/ChannelProbeWindowStagger.vi"/>
			<Item Name="lvanlys.dll" Type="Document" URL="/&lt;resource&gt;/lvanlys.dll"/>
			<Item Name="NoiseGenerator.Globals.vi" Type="VI" URL="../LV Source/SubVIs/NoiseGenerator.Globals.vi"/>
			<Item Name="NoiseGenerator.Params.ctl" Type="VI" URL="../LV Source/Typedefs/NoiseGenerator.Params.ctl"/>
			<Item Name="PipeLogic.lvclass" Type="LVClass" URL="/&lt;resource&gt;/ChannelSupport/_ChannelSupport/PipeLogic/PipeLogic.lvclass"/>
			<Item Name="ProbeFormatting.vi" Type="VI" URL="/&lt;resource&gt;/ChannelSupport/_ChannelSupport/ProbeSupport/ProbeFormatting.vi"/>
			<Item Name="Set Window Height.vi" Type="VI" URL="../../../../../epl-vi-lib/Utility VIs/Windows VIs/Set Window Height.vi"/>
			<Item Name="Set Window Position (left, top).vi" Type="VI" URL="../../../../../epl-vi-lib/Utility VIs/Windows VIs/Set Window Position (left, top).vi"/>
			<Item Name="Set Window Width.vi" Type="VI" URL="../../../../../epl-vi-lib/Utility VIs/Windows VIs/Set Window Width.vi"/>
			<Item Name="Stream-a[.](wfm(sgl)).lvlib" Type="Library" URL="/&lt;extravilib&gt;/ChannelInstances/Stream-a[.](wfm(sgl)).lvlib"/>
			<Item Name="Update Probe Details String.vi" Type="VI" URL="/&lt;resource&gt;/ChannelSupport/_ChannelSupport/ProbeSupport/Update Probe Details String.vi"/>
		</Item>
		<Item Name="Build Specifications" Type="Build">
			<Item Name="OPP.AudioStreamer" Type=".NET Interop Assembly">
				<Property Name="App_copyErrors" Type="Bool">true</Property>
				<Property Name="App_INI_aliasGUID" Type="Str">{82DAB582-4B64-410B-8886-DE3EFD37EC6E}</Property>
				<Property Name="App_INI_GUID" Type="Str">{E1A67426-A5D9-486F-934E-8FC0579950AA}</Property>
				<Property Name="App_serverConfig.httpPort" Type="Int">8002</Property>
				<Property Name="App_serverType" Type="Int">0</Property>
				<Property Name="Bld_buildCacheID" Type="Str">{AD822594-8F70-413D-9B76-976A2748AC55}</Property>
				<Property Name="Bld_buildSpecName" Type="Str">OPP.AudioStreamer</Property>
				<Property Name="Bld_excludeInlineSubVIs" Type="Bool">true</Property>
				<Property Name="Bld_excludeLibraryItems" Type="Bool">true</Property>
				<Property Name="Bld_excludePolymorphicVIs" Type="Bool">true</Property>
				<Property Name="Bld_localDestDir" Type="Path">../Build</Property>
				<Property Name="Bld_localDestDirType" Type="Str">relativeToProject</Property>
				<Property Name="Bld_modifyLibraryFile" Type="Bool">true</Property>
				<Property Name="Bld_previewCacheID" Type="Str">{7EDFCD55-DED4-4F23-BD28-439D76D63821}</Property>
				<Property Name="Bld_version.major" Type="Int">1</Property>
				<Property Name="Bld_version.minor" Type="Int">2</Property>
				<Property Name="Destination[0].destName" Type="Str">OPP.AudioStreamer.dll</Property>
				<Property Name="Destination[0].path" Type="Path">../Build/NI_AB_PROJECTNAME.dll</Property>
				<Property Name="Destination[0].path.type" Type="Str">relativeToProject</Property>
				<Property Name="Destination[0].preserveHierarchy" Type="Bool">true</Property>
				<Property Name="Destination[0].type" Type="Str">App</Property>
				<Property Name="Destination[1].destName" Type="Str">Support Directory</Property>
				<Property Name="Destination[1].path" Type="Path">../Build</Property>
				<Property Name="Destination[1].path.type" Type="Str">relativeToProject</Property>
				<Property Name="DestinationCount" Type="Int">2</Property>
				<Property Name="DotNET2011CompatibilityMode" Type="Bool">false</Property>
				<Property Name="DotNETAssembly_ClassName" Type="Str">AudioStreamer</Property>
				<Property Name="DotNETAssembly_delayOSMsg" Type="Bool">true</Property>
				<Property Name="DotNETAssembly_Namespace" Type="Str">OPP</Property>
				<Property Name="DotNETAssembly_signAssembly" Type="Bool">false</Property>
				<Property Name="DotNETAssembly_StrongNameKeyFileItemID" Type="Ref"></Property>
				<Property Name="DotNETAssembly_StrongNameKeyGUID" Type="Str">{CB972244-F220-4258-906A-C0761A154BEC}</Property>
				<Property Name="Source[0].itemID" Type="Str">{1A3E29B9-9B37-42D0-B45B-2C5B6AB6E877}</Property>
				<Property Name="Source[0].type" Type="Str">Container</Property>
				<Property Name="Source[1].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">visible</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]MethodName" Type="Str">Initialize</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]VIName" Type="Str">AudioStreamer.Initialize.vi</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">success</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[1].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">3</Property>
				<Property Name="Source[1].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Initialize.vi</Property>
				<Property Name="Source[1].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[1].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[10].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">IsTriggered</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.IsTriggered.vi</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">triggered</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[10].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[10].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.IsTriggered.vi</Property>
				<Property Name="Source[10].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[10].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[11].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">IsOpen</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.IsOpen.vi</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">success</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[11].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[11].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.IsOpen.vi</Property>
				<Property Name="Source[11].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[11].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[12].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">GetConfig</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.Get Config.vi</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">config</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[12].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[12].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Get Config.vi</Property>
				<Property Name="Source[12].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[12].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[13].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">I32</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">0</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">nreps</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">11</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">6</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">11</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">error__32in__32__40no__32error__41</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]MethodName" Type="Str">SetNumReps</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]VIName" Type="Str">AudioStreamer.Set Num Reps.vi</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[13].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">4</Property>
				<Property Name="Source[13].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Set Num Reps.vi</Property>
				<Property Name="Source[13].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[13].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[14].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Array</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">EnumerateMicrophones</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.EnumerateMicrophones.vi</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Array</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">devices</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[14].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[14].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.EnumerateMicrophones.vi</Property>
				<Property Name="Source[14].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[14].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[15].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Array</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">EnumerateOutputDevices</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.EnumerateOutputDevices.vi</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Array</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">devices</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[15].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[15].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.EnumerateOutputDevices.vi</Property>
				<Property Name="Source[15].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[15].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[16].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">device</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]MethodName" Type="Str">IsMicDeviceValid</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]VIName" Type="Str">AudioStreamer.IsMicDeviceValid.vi</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">result</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[16].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">3</Property>
				<Property Name="Source[16].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.IsMicDeviceValid.vi</Property>
				<Property Name="Source[16].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[16].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[17].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">device</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]MethodName" Type="Str">IsOutputDeviceValid</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]VIName" Type="Str">AudioStreamer.IsOutputDeviceValid.vi</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">result</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[17].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">3</Property>
				<Property Name="Source[17].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.IsOutputDeviceValid.vi</Property>
				<Property Name="Source[17].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[17].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[18].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">IsTrainer</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.IsTrainer.vi</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">trainer</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[18].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[18].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.IsTrainer.vi</Property>
				<Property Name="Source[18].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[18].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[19].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">0</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">destination</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">5</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">5</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">name</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">Array</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">data</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[4]VIProtoConNum" Type="Int">11</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[4]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[4]VIProtoDir" Type="Int">6</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[4]VIProtoIutputIdx" Type="Int">11</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[4]VIProtoName" Type="Str">error__32in__32__40no__32error__41</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[4]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]MethodName" Type="Str">SetSignal</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]VIName" Type="Str">AudioStreamer.Set Signal.vi</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfo[5]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[19].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">6</Property>
				<Property Name="Source[19].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Set Signal.vi</Property>
				<Property Name="Source[19].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[19].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[2].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">output</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">5</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">5</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">mic</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">waverMic</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[4]VIProtoConNum" Type="Int">9</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[4]VIProtoDataType" Type="Str">DBL</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[4]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[4]VIProtoIutputIdx" Type="Int">9</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[4]VIProtoName" Type="Str">Fs_Hz</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[4]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[5]VIProtoConNum" Type="Int">12</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[5]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[5]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[5]VIProtoIutputIdx" Type="Int">12</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[5]VIProtoName" Type="Str">training</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[5]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[6]VIProtoConNum" Type="Int">11</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[6]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[6]VIProtoDir" Type="Int">6</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[6]VIProtoIutputIdx" Type="Int">11</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[6]VIProtoName" Type="Str">error__32in__32__40no__32error__41</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[6]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]MethodName" Type="Str">SetConfig</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]VIName" Type="Str">AudioStreamer.Set Config.vi</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfo[7]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[2].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">8</Property>
				<Property Name="Source[2].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Set Config.vi</Property>
				<Property Name="Source[2].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[2].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[20].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">trainer</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">11</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">6</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">11</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">error__32in__32__40no__32error__41</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]MethodName" Type="Str">TrainTest</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]VIName" Type="Str">AudioStreamer.TrainTest.vi</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[20].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">4</Property>
				<Property Name="Source[20].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.TrainTest.vi</Property>
				<Property Name="Source[20].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[20].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[21].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Array</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">data</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">11</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">6</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">11</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">error__32in__32__40no__32error__41</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]MethodName" Type="Str">SendTTS</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]VIName" Type="Str">AudioStreamer.SendTTS.vi</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[21].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">4</Property>
				<Property Name="Source[21].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.SendTTS.vi</Property>
				<Property Name="Source[21].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[21].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[22].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">CloseVI</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.CloseVI.vi</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[22].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[22].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.CloseVI.vi</Property>
				<Property Name="Source[22].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[22].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[3].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">destination</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">Array</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">data</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">5</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">3</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">name</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[4]VIProtoConNum" Type="Int">11</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[4]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[4]VIProtoDir" Type="Int">6</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[4]VIProtoIutputIdx" Type="Int">11</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[4]VIProtoName" Type="Str">error__32in__32__40no__32error__41</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[4]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]MethodName" Type="Str">SetTrainer</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]VIName" Type="Str">AudioStreamer.Set Trainer.vi</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfo[5]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[3].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">6</Property>
				<Property Name="Source[3].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Set Trainer.vi</Property>
				<Property Name="Source[3].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[3].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[4].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">Start</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.Start.vi</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[4].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[4].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Start.vi</Property>
				<Property Name="Source[4].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[4].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[5].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">Stop</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.Stop.vi</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[5].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[5].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Stop.vi</Property>
				<Property Name="Source[5].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[5].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[6].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">Close</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.Close.vi</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">success</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[6].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[6].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Close.vi</Property>
				<Property Name="Source[6].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[6].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[7].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">GetErrorMessage</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.Get Error Message.vi</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">message</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[7].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[7].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Get Error Message.vi</Property>
				<Property Name="Source[7].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[7].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[8].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">0</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]MethodName" Type="Str">GetStatus</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]VIName" Type="Str">AudioStreamer.Get Status.vi</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">0</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">String</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">4</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">status</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[8].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">2</Property>
				<Property Name="Source[8].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Get Status.vi</Property>
				<Property Name="Source[8].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[8].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="Source[9].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[0]VIProtoConNum" Type="Int">-1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDataType" Type="Str">void</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[0]VIProtoDir" Type="Int">1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[0]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[0]VIProtoName" Type="Str">returnvalue</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[0]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[1]VIProtoConNum" Type="Int">7</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDataType" Type="Str">Bool</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[1]VIProtoDir" Type="Int">0</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[1]VIProtoIutputIdx" Type="Int">7</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[1]VIProtoName" Type="Str">Signal</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[1]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[2]VIProtoConNum" Type="Int">11</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[2]VIProtoDir" Type="Int">6</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[2]VIProtoIutputIdx" Type="Int">11</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[2]VIProtoName" Type="Str">error__32in__32__40no__32error__41</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[2]VIProtoOutputIdx" Type="Int">-1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]MethodName" Type="Str">Trigger</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]VIName" Type="Str">AudioStreamer.Trigger.vi</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]VIProtoConNum" Type="Int">15</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDataType" Type="Str">Cluster</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]VIProtoDir" Type="Int">7</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]VIProtoIutputIdx" Type="Int">-1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]VIProtoName" Type="Str">error__32out</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfo[3]VIProtoOutputIdx" Type="Int">15</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfoVIDocumentation" Type="Str"></Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfoVIDocumentationEnabled" Type="Int">0</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfoVIIsNamesSanitized" Type="Int">1</Property>
				<Property Name="Source[9].ExportedAssemblyVI.VIProtoInfoVIProtoItemCount" Type="Int">4</Property>
				<Property Name="Source[9].itemID" Type="Ref">/My Computer/LV Source/Exported VIs/AudioStreamer.Trigger.vi</Property>
				<Property Name="Source[9].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[9].type" Type="Str">ExportedAssemblyVI</Property>
				<Property Name="SourceCount" Type="Int">23</Property>
				<Property Name="TgtF_companyName" Type="Str">Mass General Brigham</Property>
				<Property Name="TgtF_fileDescription" Type="Str">OPP.AudioStreamer</Property>
				<Property Name="TgtF_internalName" Type="Str">OPP.AudioStreamer</Property>
				<Property Name="TgtF_legalCopyright" Type="Str">Copyright © 2023 Mass General Brigham</Property>
				<Property Name="TgtF_productName" Type="Str">OPP.AudioStreamer</Property>
				<Property Name="TgtF_targetfileGUID" Type="Str">{10F4F1C9-C77B-4731-B78A-AE5E56201341}</Property>
				<Property Name="TgtF_targetfileName" Type="Str">OPP.AudioStreamer.dll</Property>
				<Property Name="TgtF_versionIndependent" Type="Bool">true</Property>
			</Item>
			<Item Name="Query" Type="EXE">
				<Property Name="App_copyErrors" Type="Bool">true</Property>
				<Property Name="App_INI_aliasGUID" Type="Str">{3C0F2D25-56D8-4266-951B-E55AE6CBB4AD}</Property>
				<Property Name="App_INI_GUID" Type="Str">{8FA5ED32-E6D1-4F2B-8B9D-106C0D259887}</Property>
				<Property Name="App_serverConfig.httpPort" Type="Int">8002</Property>
				<Property Name="App_serverType" Type="Int">0</Property>
				<Property Name="Bld_autoIncrement" Type="Bool">true</Property>
				<Property Name="Bld_buildCacheID" Type="Str">{8D2668B2-7378-411D-B675-5554F2F93C34}</Property>
				<Property Name="Bld_buildSpecName" Type="Str">Query</Property>
				<Property Name="Bld_excludeInlineSubVIs" Type="Bool">true</Property>
				<Property Name="Bld_excludeLibraryItems" Type="Bool">true</Property>
				<Property Name="Bld_excludePolymorphicVIs" Type="Bool">true</Property>
				<Property Name="Bld_localDestDir" Type="Path">../Build</Property>
				<Property Name="Bld_localDestDirType" Type="Str">relativeToProject</Property>
				<Property Name="Bld_modifyLibraryFile" Type="Bool">true</Property>
				<Property Name="Bld_previewCacheID" Type="Str">{A6BCA73B-1EED-4534-B1AE-077268AABD0D}</Property>
				<Property Name="Bld_version.build" Type="Int">1</Property>
				<Property Name="Bld_version.major" Type="Int">1</Property>
				<Property Name="Destination[0].destName" Type="Str">QueryApplication.exe</Property>
				<Property Name="Destination[0].path" Type="Path">../Build/QueryApplication.exe</Property>
				<Property Name="Destination[0].path.type" Type="Str">relativeToProject</Property>
				<Property Name="Destination[0].preserveHierarchy" Type="Bool">true</Property>
				<Property Name="Destination[0].type" Type="Str">App</Property>
				<Property Name="Destination[1].destName" Type="Str">Support Directory</Property>
				<Property Name="Destination[1].path" Type="Path">../Build</Property>
				<Property Name="Destination[1].path.type" Type="Str">relativeToProject</Property>
				<Property Name="DestinationCount" Type="Int">2</Property>
				<Property Name="Source[0].itemID" Type="Str">{54B63487-6F76-4703-832D-2C99F1121669}</Property>
				<Property Name="Source[0].type" Type="Str">Container</Property>
				<Property Name="Source[1].destinationIndex" Type="Int">0</Property>
				<Property Name="Source[1].itemID" Type="Ref">/My Computer/LV Source/Test VIs/Query Configure.vi</Property>
				<Property Name="Source[1].sourceInclusion" Type="Str">TopLevel</Property>
				<Property Name="Source[1].type" Type="Str">VI</Property>
				<Property Name="SourceCount" Type="Int">2</Property>
				<Property Name="TgtF_companyName" Type="Str">Mass Eye &amp; Ear</Property>
				<Property Name="TgtF_fileDescription" Type="Str">Query</Property>
				<Property Name="TgtF_internalName" Type="Str">Query</Property>
				<Property Name="TgtF_legalCopyright" Type="Str">Copyright © 2026 Mass Eye &amp; Ear</Property>
				<Property Name="TgtF_productName" Type="Str">Query</Property>
				<Property Name="TgtF_targetfileGUID" Type="Str">{0D26AEB0-E514-4429-AD46-A960F1322DEC}</Property>
				<Property Name="TgtF_targetfileName" Type="Str">QueryApplication.exe</Property>
				<Property Name="TgtF_versionIndependent" Type="Bool">true</Property>
			</Item>
		</Item>
	</Item>
</Project>
