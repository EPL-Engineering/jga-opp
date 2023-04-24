import wx

# NOTE: caller needs to init like this:
#settings.nostepParams = nostepParams
#settings.topParams = topParams
#settings.stimTypes = stimTypes
#settings.stimParams = stimParams
#settings.commonParams = commonParams
#settings.placeholders = placeholders
#settings.chanNames = chanNames

radio_xy = 1    # 0=x,y act like checkboxes (allow co-vary) 1=x,y act like radio buttons LATER: user-controlled

class SettingsPanel(wx.Panel):
    def __init__(self, parent, frame, varStimFirst,varStimStep,varStimLast):
        # NOTE: varStimFirst,varStimStep,varStimLast are READ-ONLY, except for GetData()
        wx.Panel.__init__(self, parent, -1)

        inner = Settings_inner(self, frame, 1, varStimFirst,varStimStep,varStimLast)

class SettingsDialog(wx.Dialog):
    def __init__(self, parent, varStimFirst,varStimStep,varStimLast):
        # NOTE: varStimFirst,varStimStep,varStimLast are READ-ONLY, except for GetData()
        wx.Dialog.__init__(self, parent, -1, 'Settings')

        self.inner = Settings_inner(self, parent, 0, varStimFirst,varStimStep,varStimLast)
    def GetData(self):
        self.inner.GetData()

class Settings_inner(wx.Object):
    def __init__(self, parent, frame, isRO, varStimFirst,varStimStep,varStimLast):
        # NOTE: varStimFirst,varStimStep,varStimLast are READ-ONLY, except for GetData()

        bStepThenLast = 1

        self.parent = parent
        self.frame = frame
        self.varStimFirst = varStimFirst
        self.varStimStep = varStimStep
        self.varStimLast = varStimLast
        self.parent.selected = {'x':[], 'y':[]} # for internal use only

        sizer = wx.BoxSizer(wx.VERTICAL)
        sizerCh1Ch2 = wx.BoxSizer(wx.HORIZONTAL)
        border = 15
        grid_vgap = 3   # vertical gap between rows used for gridbagsizer
        grid_hgap = 5   # horizontal

        nSizeNumBox1 = 60 # 45    # n pix across for number text field (TOP SECTION)
        nSizeNumBox  = 37    # n pix across for number text field (CHAN SECTION)
        nSizeTextBox = 110

        # storage for the controls (checkboxes, text fields, etc)
        parent.isStep = {'x':{}, 'y':{}}
        parent.first = {}
        parent.last = {}
        parent.step = {}

        # ------------- 'nostep' parameters ----------------

        parent.first['nostep'] = {}
        parent.last['nostep'] = {}
        parent.step['nostep'] = {}

        gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
        row = 0
        col_lab = 0
        col_first = 1
        col_units = 2
        for param,units,convFactor in nostepParams:
            if convFactor == 'CHECKBOX':
                parent.first['nostep'][param] = wx.CheckBox(parent, -1, param, size=(-1,-1))
                if not varStimFirst['nostep'].has_key(param):
                    self.frame.WarnDlg('Missing value for "'+param+'". I will set it to 0. Please check it.')
                    varStimFirst['nostep'][param] = 0
                parent.first['nostep'][param].SetValue(varStimFirst['nostep'][param])
                col = col_lab
                col_span = 2
            elif convFactor == 'TEXT':
                gbs.Add( wx.StaticText(parent, -1, param),
                         (row,col_lab), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                if not varStimFirst['nostep'].has_key(param):
                    self.frame.WarnDlg('Missing value for "'+param+'". I will set it to blank. Please check it.')
                    varStimFirst['nostep'][param] = ''
                parent.first['nostep'][param] = wx.TextCtrl(parent, -1, varStimFirst['nostep'][param],
                                                         size=(nSizeTextBox,-1))
                col = col_first
                col_span = 1
            elif convFactor == 'FILENAME':
                gbs.Add( wx.StaticText(parent, -1, param),
                         (row,col_lab), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                if not varStimFirst['nostep'].has_key(param):
                    # self.frame.WarnDlg('Missing value for "'+param+'". I will set it to blank. Please check it.')
                    varStimFirst['nostep'][param] = ''
                parent.first['nostep'][param] = wx.TextCtrl(parent, -1, varStimFirst['nostep'][param],
                                                         size=(nSizeTextBox+50,-1))
                col = col_first
                col_span = 2
            else:
                gbs.Add( wx.StaticText(parent, -1, param),
                         (row,col_lab), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                try:
                    varFirst = varStimFirst['nostep'][param] / convFactor
                except KeyError:
                    self.frame.WarnDlg('Missing value for "'+param+'". I will set it to 0. Please check it.')
                    varStimFirst['nostep'][param] = 0
                    varFirst = 0
                parent.first['nostep'][param] = wx.TextCtrl(parent, -1, '%g' % varFirst,
                                                         size=(nSizeNumBox1,-1))
                col = col_first
                col_span = 1
            if isRO:
                parent.first['nostep'][param].Disable()
                
            gbs.Add( parent.first['nostep'][param],
                     (row,col), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            if len(units) > 0:
                gbs.Add( wx.StaticText(parent, -1, units),
                         (row,col_units), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

        sizer.Add(gbs, 0, wx.ALIGN_CENTRE|wx.ALL, border)

        # ------------- 'top' stepable parameters (not ch1 or ch2) ----------------
        parent.isStep['x']['top'] = {}
        parent.isStep['y']['top'] = {}
        parent.first['top'] = {}
        parent.last['top'] = {}
        parent.step['top'] = {}
        parent.placeholderLabel = {}
        parent.placeholderUnits = {}

        gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
        row = 0
        col_xy = {'x':0, 'y':1} # col_x=0, col_y=1
        col_lab = 2
        col_first = 3
        col_last = 4
        col_step = 5
        col_units = 6
        if bStepThenLast:
            i = col_step
            col_step = col_last
            col_last = i

        if len(topParams) > 0:
            # column labels
            gbs.Add( wx.StaticText(parent, -1, "X"),
                     (row,col_xy['x']), (1,1), wx.ALIGN_CENTER|wx.ALL)
            gbs.Add( wx.StaticText(parent, -1, "Y"),
                     (row,col_xy['y']), (1,1), wx.ALIGN_CENTER|wx.ALL)

            gbs.Add( wx.StaticText(parent, -1, " First "),
                     (row,col_first), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            gbs.Add( wx.StaticText(parent, -1, " Last "),
                     (row,col_last), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            gbs.Add( wx.StaticText(parent, -1, " Step "),
                     (row,col_step), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

        for param,units,convFactor in topParams:
            # rate (stimuli / sec)
            # X,Y checkboxes
            for xy in ['x', 'y']:
                parent.isStep[xy]['top'][param] = wx.CheckBox(parent, -1, "", size=(-1,-1))
                if isRO:
                    parent.isStep[xy]['top'][param].Disable()
                parent.Bind(wx.EVT_CHECKBOX, self.EvtXYCheckbox, parent.isStep[xy]['top'][param])
                gbs.Add( parent.isStep[xy]['top'][param], (row,col_xy[xy]), (1,1), wx.ALIGN_CENTER|wx.ALL)

            # label
            gbs.Add( wx.StaticText(parent, -1, param),
                     (row,col_lab), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            varFirst = varStimFirst['top'][param]

            s = ''
            need_pre_comma = False
            if isinstance(varFirst, list):
                for val in varFirst:
                    if need_pre_comma:
                        s += ','
                    s += '%g' % (val / convFactor)
                    need_pre_comma = True
            else:
                s = '%g' % (varFirst / convFactor)

            parent.first['top'][param] = wx.TextCtrl(parent, -1, s,
                                                     size=(nSizeNumBox1,-1))
            if isRO:
                parent.first['top'][param].Disable()
            gbs.Add( parent.first['top'][param],
                     (row,col_first), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            parent.last['top'][param] = wx.TextCtrl(parent, -1, '', size=(nSizeNumBox1,-1))
            if isRO:
                parent.last['top'][param].Disable()
            gbs.Add( parent.last['top'][param],
                     (row,col_last), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            parent.step['top'][param] = wx.TextCtrl(parent, -1, '', size=(nSizeNumBox1,-1))
            if isRO:
                parent.step['top'][param].Disable()
            gbs.Add( parent.step['top'][param],
                     (row,col_step), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            gbs.Add( wx.StaticText(parent, -1, units),
                     (row,col_units), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

        sizer.Add(gbs, 0, wx.ALIGN_CENTRE|wx.ALL, border)

        # ------------- chan 1,2 OR standard/oddball ----------------

        parent.stimType = {}
        parent.enable = {}

        col_xy = {'x':0, 'y':1} # col_x=0, col_y=1
        col_lab = 2
        col_first = 3
        col_last = 4
        col_step = 5
        col_units = 6
        n_cols = 7
        copy_button_width = 2
        if bStepThenLast:
            i = col_step
            col_step = col_last
            col_last = i

        for chan in range(2):
            if not varStimFirst.has_key(chan):
                continue
            gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
            row = 0

            if varStimFirst[chan].has_key('Enable'):
                # checkbox to enable channel
                parent.enable[chan] = wx.CheckBox(parent, -1, 'Enable', size=(-1,-1))
                parent.enable[chan].SetValue(varStimFirst[chan]['Enable'])
                if isRO:
                    parent.enable[chan].Disable()
                #parent.Bind(wx.EVT_CHECKBOX, self.EvtEnableCheckbox, parent.enable[chan])
                gbs.Add( parent.enable[chan], (row,0), (1,6), wx.ALIGN_LEFT|wx.ALL)
                row += 1

            # Stim Type
            parent.stimType[chan] = wx.Choice(parent, -1, (80, 50), choices = stimTypes)
            parent.stimType[chan].SetStringSelection(varStimFirst[chan]['Type'])
            if isRO:
                parent.stimType[chan].Disable()
            parent.Bind(wx.EVT_CHOICE, self.EvtStimType, parent.stimType[chan])
            gbs.Add( parent.stimType[chan], (row,0), (1,n_cols-copy_button_width), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            if chan == 1 and not isRO:
                # copy from std
                btn = wx.Button(parent, -1, 'Copy %s' % chanNames[0])
                parent.Bind(wx.EVT_BUTTON, self.CopyCh1, btn)
                gbs.Add( btn, (row,n_cols-copy_button_width), (1,copy_button_width), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            row += 1

            # column labels
            gbs.Add( wx.StaticText(parent, -1, "X"),
                     (row,col_xy['x']), (1,1), wx.ALIGN_CENTER|wx.ALL)
            gbs.Add( wx.StaticText(parent, -1, "Y"),
                     (row,col_xy['y']), (1,1), wx.ALIGN_CENTER|wx.ALL)

            gbs.Add( wx.StaticText(parent, -1, " First "),
                     (row,col_first), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            gbs.Add( wx.StaticText(parent, -1, " Last "),
                     (row,col_last), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            gbs.Add( wx.StaticText(parent, -1, " Step "),
                     (row,col_step), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1
            
            parent.isStep['x'][chan] = {}
            parent.isStep['y'][chan] = {}
            parent.first[chan] = {}
            parent.last[chan] = {}
            parent.step[chan] = {}

            # place holders P1-4
            parent.placeholderLabel[chan] = {}
            parent.placeholderUnits[chan] = {}
            for param in placeholders:
                bWavFileNamesField = varStimFirst[chan]['Type'] == 'Wav' and param == 'P1'
                # X checkbox
                for xy in ['x', 'y']:
                    parent.isStep[xy][chan][param] = wx.CheckBox(parent, -1, "", size=(-1,-1))
                    if isRO:
                        parent.isStep[xy][chan][param].Disable()
                    parent.Bind(wx.EVT_CHECKBOX, self.EvtXYCheckbox, parent.isStep[xy][chan][param])
                    gbs.Add( parent.isStep[xy][chan][param], (row,col_xy[xy]), (1,1), wx.ALIGN_CENTER|wx.ALL)

                parent.placeholderLabel[chan][param] = wx.StaticText(parent, -1, 'xxxxxxxxxx')
                gbs.Add( parent.placeholderLabel[chan][param],
                         (row,col_lab), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                if bWavFileNamesField:
                    sz = nSizeNumBox*5
                    col_span = 1 # should be 4, and disable the creation of step,last,units
                    # (see "bWavFileNamesField"): problem: if code fully enabled, and you switch from
                    # wav to sine, the step,last fields do not get shown
                else:
                    sz = nSizeNumBox
                    col_span = 1
                parent.first[chan][param] = wx.TextCtrl(parent, -1, '', size=(sz,-1))
                if isRO:
                    parent.first[chan][param].Disable()
                gbs.Add( parent.first[chan][param],
                         (row,col_first), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

                parent.last[chan][param] = wx.TextCtrl(parent, -1, '', size=(nSizeNumBox,-1))
                if isRO:
                    parent.last[chan][param].Disable()
                if 1: # not bWavFileNamesField:
                    gbs.Add( parent.last[chan][param],
                             (row,col_last), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

                parent.step[chan][param] = wx.TextCtrl(parent, -1, '', size=(nSizeNumBox,-1))
                if isRO:
                    parent.step[chan][param].Disable()
                if 1: # not bWavFileNamesField:
                    gbs.Add( parent.step[chan][param],
                             (row,col_step), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

                parent.placeholderUnits[chan][param] = wx.StaticText(parent, -1, 'xxxxx')
                if 1: # not bWavFileNamesField:
                    gbs.Add( parent.placeholderUnits[chan][param],
                             (row,col_units), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1
            self.UpdatePlaceholders(chan, varStimFirst[chan]['Type'])

            for param,units,convFactor in commonParams:
                if param == 'Level':
                    # skip a row, so that the empty row is a visual divider
                    row += 1
                    
                # X checkbox
                for xy in ['x', 'y']:
                    parent.isStep[xy][chan][param] = wx.CheckBox(parent, -1, '', size=(-1,-1))
                    if isRO:
                        parent.isStep[xy][chan][param].Disable()
                    parent.Bind(wx.EVT_CHECKBOX, self.EvtXYCheckbox, parent.isStep[xy][chan][param])
                    gbs.Add( parent.isStep[xy][chan][param], (row,col_xy[xy]), (1,1), wx.ALIGN_CENTER|wx.ALL)

                gbs.Add( wx.StaticText(parent, -1, param),
                         (row,col_lab), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                varFirst = varStimFirst[chan][param] / convFactor
                parent.first[chan][param] = wx.TextCtrl(parent, -1, '%g' % varFirst,
                                                        size=(nSizeNumBox,-1))
                if isRO:
                    parent.first[chan][param].Disable()
                gbs.Add( parent.first[chan][param],
                         (row,col_first), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

                parent.last[chan][param] = wx.TextCtrl(parent, -1, '', size=(nSizeNumBox,-1))
                if isRO:
                    parent.last[chan][param].Disable()
                gbs.Add( parent.last[chan][param],
                         (row,col_last), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

                parent.step[chan][param] = wx.TextCtrl(parent, -1, '', size=(nSizeNumBox,-1))
                if isRO:
                    parent.step[chan][param].Disable()
                gbs.Add( parent.step[chan][param],
                         (row,col_step), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

                gbs.Add( wx.StaticText(parent, -1, units),
                         (row,col_units), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1

            boxName = chanNames[chan]
            staticBox = wx.StaticBox(parent, -1, boxName)
            staticBoxSizer = wx.StaticBoxSizer(staticBox, wx.VERTICAL)
            staticBoxSizer.Add(gbs, 0, wx.ALIGN_CENTRE|wx.ALL, 10)

            # print 'ecs:', gbs.GetEmptyCellSize()
            gbs.SetEmptyCellSize((10,8)) # default is (10,20) - set empty row height to 8 pix

            sizerCh1Ch2.Add(staticBoxSizer, 0, wx.ALIGN_CENTRE|wx.ALL, border)

        # now add to the main sizer
        sizer.Add(sizerCh1Ch2, 0, wx.ALIGN_CENTRE|wx.ALL, border)

        self.InitControls() # hide Last and Step controls

        # --- init xy vars, checkboxes and text fields for any step and last variables we may have ---
        for xy in ['x', 'y']:
            for ii in range(len(varStimStep[xy])):
                group,param,value = varStimStep[xy][ii]
                place = param # default. correct unless using placeholder
                convFactor = 0 # init to illegal value
                if group == 'top':
                    for p,u,c in topParams:
                        if p == param:
                            convFactor = c
                            break
                #elif group == commonParams
                else: # group == 0 or group == 1:
                    stimType = varStimFirst[group]['Type']
                    for i in range(len(stimParams[stimType])):
                        if param == stimParams[stimType][i][0]:
                            # we are using a placeholder
                            place = placeholders[i]
                            convFactor = stimParams[stimType][i][2]
                    if convFactor == 0:
                        for p,u,c in commonParams:
                            if p == param:
                                convFactor = c
                                break
                self.parent.selected[xy].append((place,group))
                self.parent.isStep[xy][group][place].SetValue(1)
                if convFactor == 'TEXT':
                    continue
                if convFactor == 0:
                    print 'ERROR: no conv factor !!'
                else:
                    value /= convFactor
                #
                parent.step[group][place].SetValue('%g' % value)
                parent.step[group][place].Show(1)

                # now take care of Last. Since it has same structure as
                # Step, we can take a shortcut (just use same index ii)
                # also, the the checkboxes are already done because
                # Step had them defined too.
                groupLast,paramLast,valueLast = varStimLast[xy][ii]
                if groupLast != group:
                    print 'ERROR 1'
                if paramLast != param:
                    print 'ERROR 2'
                if convFactor != 0:
                    valueLast /= convFactor
                parent.last[group][place].SetValue('%g' % valueLast)
                parent.last[group][place].Show(1)

        # ------------- last section - OK/Cancel buttons ------

        if not isRO:
            # normal dialog (RW)
            box = wx.BoxSizer(wx.HORIZONTAL)
            btn = wx.Button(parent, wx.ID_OK, " OK ")
            btn.SetDefault()
            box.Add(btn, 0, wx.ALIGN_CENTRE|wx.ALL, 5)
            btn = wx.Button(parent, wx.ID_CANCEL, " Cancel ")
            box.Add(btn, 0, wx.ALIGN_CENTRE|wx.ALL, 5)
            sizer.Add(box, 0, wx.ALIGN_CENTER_VERTICAL|wx.ALL, border)

        parent.SetSizerAndFit(sizer)

    def InitControls(self):
        # hide Last and Step controls
        for chan in range(2):
            for param,units,convFactor in commonParams:
                self.parent.last[chan][param].Show(0) #Enable(isChecked)
                self.parent.step[chan][param].Show(0) #Enable(isChecked)

        for chan in range(2):
            for param in placeholders:
                self.parent.last[chan][param].Show(0) #Enable(isChecked)
                self.parent.step[chan][param].Show(0) #Enable(isChecked)

        for param,units,convFactor in topParams:
            self.parent.last['top'][param].Show(0) #Enable(isChecked)
            self.parent.step['top'][param].Show(0) #Enable(isChecked)

    def UpdatePlaceholders(self, chan, stimType):
        nItems = len(stimParams[stimType])
        for i in range(len(placeholders)):
            if i < nItems:
                # got something
                label,units,convFactor = stimParams[stimType][i]
                show = 1
            else:
                label = units = ''
                show = 0
            self.parent.placeholderLabel[chan][placeholders[i]].SetLabel(label)
            self.parent.placeholderUnits[chan][placeholders[i]].SetLabel(units)

            param = placeholders[i]

            if show:
                # update value
                if stimType == 'Wav':
                    if self.varStimFirst[chan].has_key(label):
                        varFirst = self.varStimFirst[chan][label]
                    else:
                        varFirst = '' # we don't have a value
                    self.parent.first[chan][param].SetValue(varFirst)
                else:
                    if self.varStimFirst[chan].has_key(label):
                        varFirst = self.varStimFirst[chan][label] / convFactor
                    else:
                        varFirst = 0 # we don't have a value, so default of 0
                    self.parent.first[chan][param].SetValue('%g' % varFirst)
            # if the placeholder is not used (show=0) and its X or Y
            # is checked, then clear it
            if not show:
                for xy in ['x', 'y']:
                    if (param,chan) in self.parent.selected[xy]:
                        self.parent.isStep[xy][chan][param].SetValue(0)
                        self.parent.selected[xy].remove((param,chan))
                        self.parent.last[chan][param].Show(0) #Enable(isChecked)
                        self.parent.step[chan][param].Show(0) #Enable(isChecked)

            # update checkboxes
            for xy in ['x', 'y']:
                self.parent.isStep[xy][chan][param].Show(show)

            # update first
            self.parent.first[chan][param].Show(show)

    def CopyCh1(self, evt):
        srcChan = 0
        dstChan = 1
        type = self.parent.stimType[srcChan].GetStringSelection()
        self.parent.stimType[dstChan].SetStringSelection(type)

        for param in placeholders:
            # todo: deal with cases where dst checkboxes are checked (last,step)
            # use UpdatePlaceholders() ??
            srcControl = self.parent.first[srcChan][param]
            if srcControl.IsShown():
                show = 1
                self.parent.placeholderLabel[dstChan][param].SetLabel(
                    self.parent.placeholderLabel[srcChan][param].GetLabel() )
                self.parent.placeholderUnits[dstChan][param].SetLabel(
                    self.parent.placeholderUnits[srcChan][param].GetLabel() )
                self.parent.first[dstChan][param].SetValue(
                    srcControl.GetValue() )
            else:
                show = 0
            for xy in ['x', 'y']:
                self.parent.isStep[xy][dstChan][param].Show(show)
            self.parent.placeholderLabel[dstChan][param].Show(show)
            self.parent.first[dstChan][param].Show(show)
            self.parent.placeholderUnits[dstChan][param].Show(show)

        for param,units,convFactor in commonParams:
            self.parent.first[dstChan][param].SetValue(
                self.parent.first[srcChan][param].GetValue() )
    
    def EvtStimType(self, evt):
        theChoiceControl = evt.GetEventObject()
        found = 0
        for chan in range(2):
            if theChoiceControl == self.parent.stimType[chan]:
                theChan = chan
                found = 1
                break
        if not found:
            print 'ERROR - Choice box not found'
            return
        stimType = evt.GetString()
        # print 'Ch',chan+1, stimType
        self.UpdatePlaceholders(chan, stimType)

    def EvtXYCheckbox(self, evt):

        # find out which checkbox just got checked or cleared
        # then deal with it
        found = 0
        theCheckbox = evt.GetEventObject()
        for xy in ['x', 'y']:
            for chan in range(2):
                for param,units,convFactor in commonParams:
                    if theCheckbox == self.parent.isStep[xy][chan][param]:
                        sel_xy = xy
                        sel_chan = chan
                        sel_param = param
                        found = 1
                        break

        if not found:
            for xy in ['x', 'y']:
                for chan in range(2):
                    for param in placeholders:
                        if theCheckbox == self.parent.isStep[xy][chan][param]:
                            sel_xy = xy
                            sel_chan = chan
                            sel_param = param
                            found = 1
                            break

        if not found:
            for xy in ['x', 'y']:
                for param,units,convFactor in topParams:
                    if theCheckbox == self.parent.isStep[xy]['top'][param]:
                        sel_xy = xy
                        sel_chan = 'top'
                        sel_param = param
                        found = 1
                        break

        if not found:
            print 'ERROR: checkbox not found.'
            return

        isChecked = theCheckbox.GetValue()

        # enable/disable last, step fields
        use_last_step_boxes = True
        if sel_chan == 'top':
            if sel_param == 'Probability':
                use_last_step_boxes = False
        else:
            if self.parent.stimType[sel_chan].GetStringSelection() == 'Wav' and \
               sel_param == 'P1':
                use_last_step_boxes = False
        if use_last_step_boxes:
            self.parent.last[sel_chan][sel_param].Show(isChecked) #Enable(isChecked)
            self.parent.step[sel_chan][sel_param].Show(isChecked) #Enable(isChecked)

        if isChecked:
            # print 'checked'
            # turn off other x/y if selected
            if sel_xy == 'x':
                other_xy = 'y'
            else:
                other_xy = 'x'
            if (sel_param,sel_chan) in self.parent.selected[other_xy]:
                # print 'CLEAR OTHER X/Y'
                self.parent.isStep[other_xy][sel_chan][sel_param].SetValue(0)
                self.parent.selected[other_xy].remove((sel_param,sel_chan))
            if radio_xy:
                # act like radio button (this can be removed to allow >1 X/Y)
                for param,chan in self.parent.selected[sel_xy]:
                    self.parent.isStep[sel_xy][chan][param].SetValue(0)
                    self.parent.selected[sel_xy].remove((param,chan))
                    self.parent.last[chan][param].Show(0) #Enable(isChecked)
                    self.parent.step[chan][param].Show(0) #Enable(isChecked)
            # update state of this checkbox (do after clearing other "radio" buttons)
            self.parent.selected[sel_xy].append((sel_param,sel_chan))
        else:
            # print 'cleared'
            self.parent.selected[sel_xy].remove((sel_param,sel_chan))

        # print 'self.parent.selected=',self.parent.selected

    def GetData(self):
        # fill in varStimFirst
        for param,units,convFactor in nostepParams:
            if convFactor == 'CHECKBOX':
                varFirst = int(self.parent.first['nostep'][param].GetValue())
            elif convFactor == 'TEXT' or convFactor == 'FILENAME':
                varFirst = self.parent.first['nostep'][param].GetValue()
            else:
                varFirst = convFactor * float(self.parent.first['nostep'][param].GetValue())
            self.varStimFirst['nostep'][param] = varFirst
        for param,units,convFactor in topParams:
            varFirst = self.parent.first['top'][param].GetValue()
            if varFirst.count(','):
                # a list of values ( e.g. probability: 10,90,50 )
                varList = []
                for val in varFirst.split(','):
                    varList.append(convFactor * float(val))
                varFirst = varList
            else:
                # single value (normal)
                varFirst = convFactor * float(varFirst)
            self.varStimFirst['top'][param] = varFirst
        for chan in range(2):
            if not self.varStimFirst.has_key(chan):
                continue
            if self.varStimFirst[chan].has_key('Enable'):
                self.varStimFirst[chan]['Enable'] = self.parent.enable[chan].GetValue()
                
            type = self.parent.stimType[chan].GetStringSelection()
            self.varStimFirst[chan]['Type'] = type
            for i in range(len(stimParams[type])): #
                # need to go indirect on these (using placeholders)
                param,units,convFactor = stimParams[type][i]
                placeholder = placeholders[i]
                if convFactor == 'TEXT':
                    varFirst = self.parent.first[chan][placeholder].GetValue()
                else:
                    varFirst = convFactor * float(self.parent.first[chan][placeholder].GetValue())
                self.varStimFirst[chan][param] = varFirst
            for param,units,convFactor in commonParams:
                varFirst = convFactor * float(self.parent.first[chan][param].GetValue())
                self.varStimFirst[chan][param] = varFirst
        # now fill in the x/y stuff
        for xy in ['x', 'y']:
            self.varStimStep[xy] = []
            self.varStimLast[xy] = []
            for param,group in self.parent.selected[xy]:
                place = param
                # get convFactor
                found = 0
                if group == 'top':
                    for p,u,c in topParams:
                        if p == param:
                            convFactor = c
                            found = 1
                            break
                else:
                    for p,u,c in commonParams:
                        if p == param:
                            convFactor = c
                            found = 1
                            break
                    if not found:
                        # is stimParam - so get it based on type, and also
                        # get actual param (instead of placeholder which is what
                        # param is at the moment)
                        type = self.parent.stimType[group].GetStringSelection()
                        for i in range(len(stimParams[type])):
                            if placeholders[i] == param:
                                param,u,convFactor = stimParams[type][i]
                                found = 1
                                break
                if not found:
                    print 'ERROR: NOT FOUND'
                if param == 'WavFiles':
                    self.varStimStep[xy].append((group,param,0))
                    self.varStimLast[xy].append((group,param,0))
                elif isinstance(self.varStimFirst[group][param], list):
                    # NEW: list of values, e.g. [10.0, 90.0, 50.0] for probability
                    self.varStimStep[xy].append((group,param,0))
                    self.varStimLast[xy].append((group,param,0))
                else:
                    try:
                        step = convFactor * float(self.parent.step[group][place].GetValue())
                    except:
                        step = 1.0
                    try:
                        last = convFactor * float(self.parent.last[group][place].GetValue())
                    except:
                        last = 1.0
                    self.varStimStep[xy].append((group,param,step))
                    self.varStimLast[xy].append((group,param,last))

#-------------------------------------------------------------------

class SettingsDialogGeneric(wx.Dialog):
    def __init__(self, parent, title, var, params):
        # NOTE: var is READ-ONLY, except for GetData()
        wx.Dialog.__init__(self, parent, -1, title)
        self.inner = SettingsGeneric_inner(self, 0, var, params)
    def GetData(self):
        self.inner.GetData()

class SettingsGeneric_inner(wx.Object):
    def __init__(self, parent, isRO, var, params):
        # NOTE: var is READ-ONLY, except for GetData()

        self.parent = parent
        self.var = var
        self.params = params

        sizer = wx.BoxSizer(wx.VERTICAL)
        border = 15
        grid_vgap = 3   # vertical gap between rows used for gridbagsizer
        grid_hgap = 5   # horizontal

        # storage for the controls (checkboxes, text fields, etc)
        parent.file = {}

        gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
        row = 0
        col_lab = 0
        col_first = 1
        col_units = 2

        for param_list in params:
            if len(param_list) == 4:
                param,units,convFactor,nWidth = param_list
                fmt = '%g'
            else:
                param,units,convFactor,nWidth,fmt = param_list
            if convFactor == 'CHECKBOX':
                v = var[param]
                #print 'v=',v,' param=',param
                parent.file[param] = wx.CheckBox(parent, -1, '', size=(nWidth,-1))
                parent.file[param].SetValue(v)
                col = col_first
                col_span = 2
            elif convFactor == 'TEXT':
                v = var[param]
                parent.file[param] = wx.TextCtrl(parent, -1, v, size=(nWidth,-1))
                col = col_first
                col_span = 1
            elif convFactor == 'STATIC_TEXT':
                row += 1
                #('Signal parameters','','STATIC_TEXT',120)
                gbs.Add( wx.StaticText(parent, -1, param),
                         (row,0), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1
                continue
            elif convFactor == 'DROP_LIST':
                # ('Masker type', '', 'DROP_LIST', 40, ['Tone','RandTone','Noise',])
                v = var[param]
                parent.file[param] = wx.ComboBox(parent, -1, value=v, size=(100, -1), choices=fmt, style=wx.CB_READONLY)
                col = col_first
                col_span = 2
            else:
                v = var[param] / convFactor
                parent.file[param] = wx.TextCtrl(parent, -1, fmt % v, size=(nWidth,-1))
                col = col_first
                col_span = 1
            if isRO:
                parent.file[param].Disable()
                
            gbs.Add( wx.StaticText(parent, -1, param+' '+units),
                     (row,col_lab), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            gbs.Add( parent.file[param],
                     (row,col), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

##            gbs.Add( wx.StaticText(parent, -1, units),
##                     (row,col_units), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

        sizer.Add(gbs, 0, wx.ALIGN_CENTRE|wx.ALL, border)

        # ------------- last section - OK/Cancel buttons ------

        if not isRO:
            # normal dialog (RW)
            box = wx.BoxSizer(wx.HORIZONTAL)
            btn = wx.Button(parent, wx.ID_OK, " OK ")
            btn.SetDefault()
            box.Add(btn, 0, wx.ALIGN_CENTRE|wx.ALL, 5)
            btn = wx.Button(parent, wx.ID_CANCEL, " Cancel ")
            box.Add(btn, 0, wx.ALIGN_CENTRE|wx.ALL, 5)
            sizer.Add(box, 0, wx.ALIGN_CENTER_VERTICAL|wx.ALL, border)

        parent.SetSizerAndFit(sizer)

    def GetData(self):
        # fill in var
        for param_list in self.params:
            if len(param_list) == 4:
                param,units,convFactor,nWidth = param_list
            else:
                param,units,convFactor,nWidth,fmt = param_list
            if convFactor == 'CHECKBOX' or convFactor == 'TEXT' or convFactor == 'DROP_LIST':
                v = self.parent.file[param].GetValue()
            elif convFactor == 'STATIC_TEXT':
                continue
            else:
                v = convFactor * float(self.parent.file[param].GetValue())
            self.var[param] = v

