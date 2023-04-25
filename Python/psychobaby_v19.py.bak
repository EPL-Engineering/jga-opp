#!/usr/bin/env python

"""
psychobaby_v9.py - code that can be used by all psychobaby software

Update the vers number when the caller must be modified.

for now do: from psychobaby import *

To execute doctests:
1. Start Windows command prompt
2. Z:
3. cd dev\py-module-dev\phys
4. python psychobaby.py (To see full output: python psychobaby.py -v)

To develop more doctests:
1. Open this file in IDLE
2. Hit F5
3. From Shell window, call function to test, like: CalcMinSigCatPro(0, 0, 7, 8, 9, 10)
"""

# v8: rename bBouncedBackToThisPhase to nBouncedBackToThisPhase
# v7: use time.clock() instead of time.time()
#
# defining ENAB_MOVIE here over-rides in parent!
#ENAB_MOVIE =  False

# import first, as these constants may be used early on
import config_phys
import hw_pb as hw

# built-ins
import os
import fnmatch
import sys
import time
import random

import wx

import ctypes

import numpy

if hw.TOY_CONTROLLER == 'FTDI':
    try:
        d2xx = ctypes.windll.ftd2xx
    except:
        print 'Software configuration error. You need to install the d2xx DLL.'
        print 'Please email %s for assistance.' % config_phys.CONTACT_EMAIL
        raw_input('Press enter to close this window.')
        sys.exit(1)
    FT_OK = 0

# locally developed modules
from phys_base import settings_v2 as settings
from phys_base.phys import *
import pb

if hw.TOY_CONTROLLER == 'TDT_RP2' or hw.devAtten  == 'TDT_PA5':
    from phys import tdt_sys3


def load_abc_wav_filenames(parent, wav_folder, list_file, max_ans=2):
    """ open file of wav filenames, format is:
        wav_A wav_B wav_C answer-1-or-2

    num_rows, A_fnames, B_fnames, C_fnames, answers = load_abc_wav_filenames(parent, wav_folder, list_file)

    >>> num_rows, A_fnames, B_fnames, C_fnames, answers = load_abc_wav_filenames(None, 'doc_and_unit_test_files', 'ABC-small.txt')
    >>> num_rows
    3
    >>> A_fnames
    ['swpt1-2.wav', 'sig1.wav', 'sig1.wav']
    >>> B_fnames
    ['swpt2-1.wav', 'sig2.wav', 'sig2.wav']
    >>> C_fnames
    ['swpt2-1.wav', 'sig2a.wav', 'sig1a.wav']
    >>> answers
    [2, 2, 1]
    """
    path = os.path.join(wav_folder, list_file)
    n_rows = 0
    A_wav_files = []
    B_wav_files = []
    C_wav_files = []
    answers = []
    try:
        f = open(path, 'rU')
        while True:
            line = f.readline()
            if not line:
                # print 'ran out of data'
                break
            line = line.rstrip() # chop off \n
            if not line:
                # print 'blank line'
                continue
            row = wav_fn_str_to_list(line)# 'a.wav', 'b.wav', 'a.wav', '1.wav'
            n = len(row)
            if n != 4:
                ErrorDlg(parent, 'ERROR: Expecting 4 columns, but got %d columns in row %d (file %s)' %
                         (n, n_rows+1, list_file))
                return 0,[],[],[],[]
            A_wav_files.append(row[0])
            B_wav_files.append(row[1])
            C_wav_files.append(row[2])
            ans = int(row[3][:-4])
            if ans < 1 or ans > max_ans:
                ErrorDlg(parent, 'ERROR: got invalid answer in row %d (file %s)' %
                         (n_rows+1, list_file))
                return 0,[],[],[],[]
            answers.append(ans)
            n_rows += 1
        f.close()
        # print n_cols,n_rows,wav_files
        return n_rows,A_wav_files,B_wav_files,C_wav_files,answers
    except:
        WarnDlg(parent, 'Error reading file of ABC wav names %s' % path)
        return 0,[],[],[],[]

def create_shuffled_intensity_list(intensities_str, block_size):
    """ convert string to list, duplicate so that there are block_size entries, shuffle

    >>> sorted(create_shuffled_intensity_list('30 40 50', 0))
    [30.0, 40.0, 50.0]
    >>> sorted(create_shuffled_intensity_list('30 40 50', 1))
    [30.0, 40.0, 50.0]
    >>> sorted(create_shuffled_intensity_list('30 40 50.5', 3))
    [30.0, 40.0, 50.5]
    >>> sorted(create_shuffled_intensity_list('30 40 50', 5))
    [30.0, 40.0, 50.0]
    >>> sorted(create_shuffled_intensity_list('30 40 50', 6))
    [30.0, 30.0, 40.0, 40.0, 50.0, 50.0]
    """
    intensity_list = []
    intensities = intensities_str.split()
    for intensity in intensities:
        inten = intensity.strip()
        if inten:
            intensity_list.append(float(inten))
    n = len(intensity_list)
    num_blocks = block_size / n
    if num_blocks < 1:
        num_blocks = 1
    shuffled_intensity_list = []
    for i in range(num_blocks):
        shuffled_intensity_list.extend(intensity_list)
    random.shuffle(shuffled_intensity_list)
    return shuffled_intensity_list

def ReadHeader(parent, bIsDataFile, f, filename, params):
    vglobals = {} # globals()
    preLine = ''
    while True:
        line = f.readline().rstrip("\r\n") # chop off \n
        if not line:
            # ran out of data
            ErrorDlg(parent, 'ERROR: ran out of data while reading header. File='+filename)
            return 1
        if line == 'END_OF_HEADER':
            if len(preLine) > 0:
                ErrorDlg(parent, 'Incomplete line in header. File='+filename)
                return 1
            break
        line = preLine + line
        preLine = ''
        if line[-1] == ' ':
            # print 'need to read another line:',line
            preLine = line
            continue
        # print line
        if line[0:5] == 'self.':
            # older file format has lines like: "self.dotDispVars = {'Dot size': 1}"
            # remove the "self."
            line = line[5:]
        try:
            exec line in vglobals, params
        except:
            ErrorDlg(parent, "ERROR: cannot understand header line %s of file %s" % (line,filename))
            return 1
    return 0

def CalcMinSigCatPro(bEnabNoSig, bEnabProbe, nCatchRatio_catch, nCatchRatio_sig, nProbeRatio_probe, nProbeRatio_sig):
    """ Calculate minumum number of signal, catch, and probe trials,
    based on catch:signal and probe:signal ratios
    
    No catch, no probe (only sig)
    >>> CalcMinSigCatPro(False, False, 7, 8, 9, 10)
    (1, 0, 0)

    Probe to sig ratio is 1:5
    >>> CalcMinSigCatPro(False, True, 100, 101, 1, 5)
    (5, 0, 1)

    Probe to sig ratio is 2:5
    >>> CalcMinSigCatPro(False, True, 100, 101, 2, 5)
    (5, 0, 2)

    Catch to sig ratio is 5:5
    >>> CalcMinSigCatPro(True, False, 5, 5, 101, 100)
    (5, 5, 0)

    Catch to sig ratio is 5:5
    Probe to sig ratio is 2:5
    >>> CalcMinSigCatPro(True, True, 5, 5, 2, 5)
    (5, 5, 2)
    """
    if bEnabNoSig:
        # make sure args are int
        nCatchRatio_catch = int(nCatchRatio_catch)
        nCatchRatio_sig   = int(nCatchRatio_sig)
    else:
        nCatchRatio_catch = 0
        nCatchRatio_sig   = 1

    if bEnabProbe:
        # make sure args are int
        nProbeRatio_probe = int(nProbeRatio_probe)
        nProbeRatio_sig   = int(nProbeRatio_sig)
    else:
        nProbeRatio_probe = 0
        nProbeRatio_sig   = 1

    nSig = lcm(nCatchRatio_sig, nProbeRatio_sig)
    nCat = nCatchRatio_catch * nSig/nCatchRatio_sig
    nPro = nProbeRatio_probe * nSig/nProbeRatio_sig

    return nSig,nCat,nPro

def CalcMinSigCatProFromDlg(dlg):
    # NOTE: dictinary keys (e.g. 'Enable No-signal trials') must match code that calls this!
    minNSig,minNCat,minNPro = CalcMinSigCatPro(dlg['Enable No-signal trials'].GetValue(), 
                                               dlg['Enable probe trials'].GetValue(), 
                                               dlg['No-sig ratio no-sig'].GetValue(), 
                                               dlg['No-sig ratio sig'].GetValue(), 
                                               dlg['Probe ratio probe'].GetValue(), 
                                               dlg['Probe ratio sig'].GetValue())
    return minNSig,minNCat,minNPro


class Toys(wx.Object):
    def __init__(self, parent, strDevice):
        self.strDevice = strDevice
        self.parent = parent
        if strDevice == 'ONTRAK ADU208':
            if config_phys.DEV_DLL:
                p = "z:\\dev\\3rdPartyDLLs\\AduHidTest32\\AduHid.dll"
            else:
                p = os.path.join(sys.prefix,'SWBaby_DLLs','AduHid.dll')
            try:
                self.dll = ctypes.WinDLL(p)
            except:
                ErrorDlg(self.parent, 'unable to load Ontrak_ADU208_relay DLL from %s' % p)
                self.strDevice = ''
                return
            self.handle = self.dll.OpenAduDevice(0)
            if self.handle < 0:
                ErrorDlg(self.parent, 'Cannot locate ONTRAK ADU208')
                self.strDevice = ''
                return
        elif strDevice == 'FTDI': # FTDI TTL-232R-3V3
            try:
                FT_LIST_NUMBER_ONLY = 0x80000000
                arg1 = ctypes.c_ulong(123) # init to 123 so I know if not changed
                stat = d2xx.FT_ListDevices(ctypes.pointer(arg1), 0, FT_LIST_NUMBER_ONLY)
                if stat != FT_OK:
                    print 'CANNOT LIST DEVICES, stat=', stat
                    ErrorDlg(self.parent, 'ERROR listing USB devices (you might need to reboot)')
                    return
                if arg1.value == 0:
                    ErrorDlg(self.parent, 'The FTDI TTL-232R-3V3 USB to serial interface not detected. Is it plugged in?')
                    return
                elif arg1.value < 0:
                    ErrorDlg(self.parent, 'Error listing FTDI devices (found less than zero!)')
                    return
                elif arg1.value > 1:
                    ErrorDlg(self.parent, "More than 1 FTDI devices found. I'll use the first one")
                    return
            except Exception, target:
                ErrorDlg(self.parent, 'ERROR listing USB devices (you might need to reboot): %s' % str(target))
                return

            self.handle = ctypes.c_ulong(0)
            if d2xx.FT_Open(0, ctypes.pointer(self.handle)) != FT_OK:
                ErrorDlg(self.parent, 'ERROR initializing d2xx toy controller.')
                self.strDevice = ''
                return
            if d2xx.FT_SetBitMode(self.handle, 14, 1) != FT_OK: # 2nd arg is mask: 1 means output. 3rd arg: 0x1 = Asynchronous Bit Bang
                ErrorDlg(self.parent, 'ERROR SETTING BIT MODE on d2xx toy controller.')
                self.strDevice = ''
                return
            # WarnDlg(self.parent, 'JUST SET BIT MODE ON FTDI')
        elif strDevice == 'TDT_RP2':
            try:
                status = tdt_sys3.initRP(parent, hw.strConnect, 'RP2', 0, 'toys.rcx', 1)
            except:
                status = 1
            if status:
                ErrorDlg(self.parent, 'ERROR initializing TDT RP2 toy controller.')
                self.strDevice = ''
                return
        elif strDevice == 'NI USB-6008':
            ErrorDlg(self.parent, 'NI USB-6008 Toy controller NOT YET IMPLEMENTED')
            self.strDevice = ''
            return
        else:
            ErrorDlg(self.parent, 'UNKNOWN TOY CONTROLLER DEVICE')
            self.strDevice = ''
            return

    def TurnOn(self, bEnabToys=True, bEnabDVD=True, toy_dur=10.0, nToy=-1): # toy_dur in seconds
### the following was added 5/5/2016, but removed the same day, as is was not asked for
##        if self.parent.analog_io:
##            print 'TURN ON TOYS: MUTE ON'
##            if self.parent.analog_io.SetAtten(9000, 9000): # -90 dB
##                ErrorDlg(self.parent, 'ERROR while trying to mute sound.')
        if self.strDevice == 'ONTRAK ADU208':
            if nToy < 0:
               nToy = random.randint(0, 1)
               
            # nToy is 0 or 1
            lpNumberOfBytesWritten = ctypes.c_long(0);
            # bit numbers: 0=toy0 1=toy1 2=toy_motor 3=DVD_video 4=DVD_audio
            # bit values:  1=toy0 2=toy1 4=toy_motor 8=DVD_video 16=DVD_audio
            word = 0
            if bEnabToys:
                word += nToy+1      # convert to bit mask (only valid if nToy is 0 or 1)
                word += 4           # turn on K2 (toy motor)
            if bEnabDVD:
                word += 24          # turn on K3 (bit 3) and K4 (bit 4) - DVD video and audio
            
            s = "MK%d" % word
            self.dll.WriteAduDevice(self.handle, s, len(s),
                                    ctypes.pointer(lpNumberOfBytesWritten), 0)
                                    
            #print 'TURN ON, wrote %d' % word
            #print 'TURN ON, wrote %d bytes' % lpNumberOfBytesWritten.value

        elif self.strDevice == 'FTDI': # FTDI TTL-232R-3V3
            nToy = random.randint(0, 1)
            # nToy is 0 or 1
            word = ctypes.c_short(0)
            num_bytes_written = ctypes.c_long(0)
            word.value = 8 # D3 = either toy
            # could be more like ONTRACK setup, allowing people to get rid of 25pin switch
            # be just like RP2 for now
            if nToy:
                word.value += 4 # D2 (2nd toy)
            else:
                word.value += 2 # D1 (1st toy)

            num_bytes_written.value = 0
            if d2xx.FT_Write(self.handle, ctypes.pointer(word), 1, ctypes.pointer(num_bytes_written)) != FT_OK: # 0x1 = Asynchronous Bit Bang
                ErrorDlg(self.parent, 'ERROR writing to d2xx toy controller.')
                self.strDevice = ''
                return

        elif self.strDevice == 'TDT_RP2':
            if not tdt_sys3.rp2[0].SetTagVal("ToyDuration", float(toy_dur*1e3)): # in millisec
                ErrorDlg(self.parent, 'TDT ERROR setting ToyDuration')
                return 1
            if not tdt_sys3.rp2[0].SoftTrg(3):
                ErrorDlg(self.parent, 'TDT ERROR sending soft trig')
                return 1
            if not tdt_sys3.rp2[0].SetTagVal("ToyEnable", 1.0):
                ErrorDlg(self.parent, 'TDT ERROR setting ToyEnable')
                return 1
        elif self.strDevice == 'NI USB-6008':
            ErrorDlg(self.parent, 'Toy controller NI USB-6008 NOT YET IMPLEMENTED')
            return 1
        else:
            ErrorDlg(self.parent, 'UNKNOWN TOY CONTROLLER')
            return 1
        return 0
        
    def TurnOff(self):
        # turn off all toys
### the following was added 5/5/2016, but removed the same day, as is was not asked for
##        if self.parent.analog_io:
##            print 'TURN OFF TOYS: MUTE OFF'
##            if self.parent.analog_io.SetAtten(0, 0):
##                ErrorDlg(self.parent, 'ERROR while trying to un-mute sound.')
        if self.strDevice == 'ONTRAK ADU208':
            lpNumberOfBytesWritten = ctypes.c_long(0);
            # s = "MK0" # just turn off all realys (the toy and the DVD)
            s = "MK32" # turn off all relays (the toy and the DVD) TURN ON K5 (shorts DVD audio input to gnd)
            self.dll.WriteAduDevice(self.handle, s, len(s),
                                    ctypes.pointer(lpNumberOfBytesWritten), 0)
        elif self.strDevice == 'FTDI': # FTDI TTL-232R-3V3
            word = ctypes.c_short(0)
            num_bytes_written = ctypes.c_long(0)
            word.value = 0

            num_bytes_written.value = 0
            if d2xx.FT_Write(self.handle, ctypes.pointer(word), 1, ctypes.pointer(num_bytes_written)) != FT_OK: # 0x1 = Asynchronous Bit Bang
                ErrorDlg(self.parent, 'ERROR writing to d2xx toy controller.')
                self.strDevice = ''
                return
            # WarnDlg(self.parent, 'JUST SET FTDI TO %d' % word.value)
        elif self.strDevice == 'TDT_RP2':
            return 0 # the TDT now turns off by itself 11/6/13
        elif self.strDevice == 'NI USB-6008':
            ErrorDlg(self.parent, 'Toy controller NI USB-6008 NOT YET IMPLEMENTED')
            return 1
        else:
            ErrorDlg(self.parent, 'UNKNOWN TOY CONTROLLER DEVICE')
            return 1
        return 0

##    def close(self):
##        if self.strDevice == 'FTDI':
##            if self.handle.value:
##                d2xx.FT_Close(self.handle)
##                print 'closed FTDI device'

class ModelessDlg(wx.Dialog):
    # non-modal dialog
    # supply title, message, and button labels
    def __init__(self, parent, title, message, buttonLabels, params=[], var={}, red=False):
        style = wx.CAPTION
        wx.Dialog.__init__(self, parent, -1, title, style=style)

        sizer = wx.BoxSizer(wx.VERTICAL)
        box = wx.BoxSizer(wx.HORIZONTAL)
        # this is where I could add an icon (e.g. INFO or RED X)
        box.Add( wx.StaticText(self, -1, message), 0, wx.ALIGN_CENTER_VERTICAL|wx.ALL, 5)
        sizer.AddSizer(box, 0, wx.GROW|wx.ALIGN_CENTER_VERTICAL|wx.ALL, 5)

        if len(params):
            self.container = self # parent
            self.var = var
            self.params = params
            
            # storage for the controls (checkboxes, text fields, etc)
            self.container.control = {}
    
            grid_vgap = 3   # vertical gap between rows used for gridbagsizer
            grid_hgap = 5   # horizontal
            gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
            row = 0
            col_lab = 0
            col_first = 1
            col_units = 2
            
            for param,units,convFactor,nWidth in params:
                if convFactor == 'CHECKBOX':
                    v = var[param]
                    self.container.control[param] = wx.CheckBox(self.container, -1, '', size=(nWidth,-1))
                    self.container.control[param].SetValue(v)
                    col = col_first
                    col_span = 2
                elif convFactor == 'CHOICE':
                    v = var[param] # instead of current setting, I pass list of options
                    #self.container.control[param] = wx.Choice(self.container, -1, size=(nWidth, -1), # another widget
                    self.container.control[param] = wx.ComboBox(self.container, -1, value='', size=(nWidth, -1),
                                                                choices=v, style=wx.CB_DROPDOWN) # CB_SIMPLE to show all at once
                    col = col_first
                    col_span = 1
                elif convFactor == 'TEXT':
                    v = var[param]
                    self.container.control[param] = wx.TextCtrl(self.container, -1, v, size=(nWidth,-1))
                    col = col_first
                    col_span = 1
                elif convFactor == 'STATIC_TEXT':
                    row += 1
                    #('Up/Down parameters','','STATIC_TEXT',120),
                    gbs.Add( wx.StaticText(self.container, -1, param),
                             (row,0), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                    row += 1
                    continue
                elif convFactor == 'INT':
                    v = var[param]
                    self.container.control[param] = wx.TextCtrl(self.container, -1, '%d' % v, size=(nWidth,-1))
                    col = col_first
                    col_span = 1
                else:
                    v = var[param] / convFactor
                    self.container.control[param] = wx.TextCtrl(self.container, -1, '%g' % v, size=(nWidth,-1))
                    col = col_first
                    col_span = 1
                    
                gbs.Add( wx.StaticText(self.container, -1, param),#+' '+units),
                         (row,col_lab), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
    
                gbs.Add( self.container.control[param],
                         (row,col), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
    
                gbs.Add( wx.StaticText(self.container, -1, units),
                         (row,col+col_span), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1
            border = 15
            sizer.AddSizer(gbs, 0, wx.GROW|wx.ALIGN_CENTER_VERTICAL|wx.ALL, 5)
        
        box = wx.BoxSizer(wx.HORIZONTAL)
        for buttonLabel in buttonLabels:
            btn = wx.Button(self, -1, buttonLabel)
            if buttonLabel == "OK":
                btn.SetDefault()
            box.Add(btn, 0, wx.ALIGN_CENTRE|wx.ALL, 5)
        sizer.Add(box, 0, wx.ALIGN_CENTER_VERTICAL|wx.ALL, 5)

        self.SetSizer(sizer)
        self.SetAutoLayout(True)
        sizer.Fit(self)

        if red:
            self.SetBackgroundColour(wx.Colour(red=255))

        self.Bind(wx.EVT_BUTTON, self.OnButt)
        self.sButt = "" # identifies button pressed

    def OnButt(self, evt):
        # does not kill dlg
        self.sButt = evt.GetEventObject().GetLabel()

### No work:
##    def OnCharHook(self, evt):
##        keycode = evt.GetKeyCode()
##        if keycode == wx.WXK_ESCAPE:
##            print 'ESC detected in ModelessDlg'
        
    def GetData(self, bad_input_value=-123.45):
        # fill in var
        for param,units,convFactor,nWidth in self.params:
            if convFactor == 'CHECKBOX':
                v = self.container.control[param].GetValue()
            elif convFactor == 'CHOICE':
                v = self.container.control[param].GetValue()
            elif convFactor == 'TEXT':
                v = self.container.control[param].GetValue()
            elif convFactor == 'STATIC_TEXT':
                continue
            elif convFactor == 'INT':
                try:
                    v = int(self.container.control[param].GetValue())
                except:
                    v = int(bad_input_value)
            else:
                try:
                    v = convFactor * float(self.container.control[param].GetValue())
                except:
                    v = float(bad_input_value)
            self.var[param] = v

class RandBlkSzValidator(wx.PyValidator):
    def __init__(self):
        wx.PyValidator.__init__(self)

    def Clone(self):
        # Every validator must implement the Clone() method.
        return RandBlkSzValidator()

    def Validate(self, win):
        textCtrl = self.GetWindow()
        rand_block_size = int(textCtrl.GetValue())

        minNSig,minNCat,minNPro = CalcMinSigCatProFromDlg(win.control)
        min_block_size = minNSig+minNCat+minNPro
##        if rand_block_size % (min_block_size) != 0:
##            wx.MessageBox('"Random block size" must be zero or a multiple of the min block size (%s)' % min_block_size, 'Error')
        if rand_block_size != 0 and rand_block_size != min_block_size:
            wx.MessageBox('"Random block size" must be zero or the min block size (%s)' % min_block_size, 'Error')
            textCtrl.SetBackgroundColour('red')
            textCtrl.SetSelection(0,-1)
            textCtrl.SetFocus()
            textCtrl.Refresh()
            return False
        else:
            return True

    def TransferToWindow(self):
        return True

    def TransferFromWindow(self):
        return True

class SettingsPhase(wx.Dialog):
    def __init__(self, frame, title, var, params, catchAndProbeVars, phase):
        # NOTE: var is READ-ONLY, except for GetData()
        self.frame = frame

        phase_info_str = ' for Phase %d' % (phase+1)
        try:
            phase_name = frame.controlPanel.phaseName[phase].GetValue()
            if phase_name:
                phase_info_str += ' ("%s")' % phase_name
        except:
            pass

        wx.Dialog.__init__(self, frame, -1, title+phase_info_str)
        self.inner = SettingsPhase_inner(frame, self, 0, var, params, catchAndProbeVars)
        self.Centre()
    def GetData(self):
        try:
            self.inner.GetData()
        except:
            ErrorDlg(self.frame, 'ERROR: reading one or more of the entries you made. PLEASE send Brandon the ERROR.txt file.')
            raise # so we get stackdump to ERROR.txt
    def CalcMinBlock(self):
        self.inner.CalcMinBlock(0)

class SettingsPhase_inner(wx.Object):
    def __init__(self, frame, parent, isRO, var, params, catchAndProbeVars):
        # NOTE: var is READ-ONLY, except for GetData()

        self.frame = frame
        self.parent = parent
        self.var = var
        self.params = params
        self.catchAndProbeVars = catchAndProbeVars

        sizer = wx.BoxSizer(wx.VERTICAL) # upper half, lower half
        sizerTopLR = wx.BoxSizer(wx.HORIZONTAL)
        sizerBottomLR = wx.BoxSizer(wx.HORIZONTAL)
        sizerTopRightTB = wx.BoxSizer(wx.VERTICAL)

        # storage for the controls (checkboxes, text fields, etc)
        parent.control = {}

        grid_vgap = 3   # vertical gap between rows used for gridbagsizer
        grid_hgap = 5   # horizontal
        gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
        row = 0
        col_lab = 0
        col_first = 1
        col_units = 2

        for param,units,convFactor,nWidth in params:
            if convFactor == 'CHECKBOX':
                v = var[param]
                #print 'v=',v,' param=',param
                parent.control[param] = wx.CheckBox(parent, -1, '', size=(nWidth,-1))
                parent.control[param].SetValue(v)
                col = col_first
                col_span = 2
            elif convFactor == 'TEXT':
                v = var[param]
                parent.control[param] = wx.TextCtrl(parent, -1, v, size=(nWidth,-1))
                col = col_first
                col_span = 1
                if nWidth > 50:
                    col_span = 2
            elif convFactor == 'STATIC_TEXT':
                row += 1
                #('Up/Down parameters','','STATIC_TEXT',120),
                gbs.Add( wx.StaticText(parent, -1, param),
                         (row,0), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1
                continue
            elif convFactor == 'INT':
                v = var[param]
                parent.control[param] = wx.TextCtrl(parent, -1, '%d' % v, size=(nWidth,-1))
                col = col_first
                col_span = 1
            else:
                v = var[param] / convFactor
                parent.control[param] = wx.TextCtrl(parent, -1, '%g' % v, size=(nWidth,-1))
                col = col_first
                col_span = 1
            if isRO:
                parent.control[param].Disable()
                
            gbs.Add( wx.StaticText(parent, -1, param),#+' '+units),
                     (row,col_lab), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            gbs.Add( parent.control[param],
                     (row,col), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            gbs.Add( wx.StaticText(parent, -1, units),
                     (row,col+col_span), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

        border = 10
        sizerTopLR.Add(gbs, 0, wx.ALIGN_TOP|wx.ALL, border)


        # top right section
        gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
        row = 0
        col = 0
        nWidth = 35
        vName = 'Random block size'
        gbs.Add( wx.StaticText(parent, -1, '%s (0=no blocking)'%vName),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        parent.control[vName] = wx.TextCtrl(parent, -1, '%g' % catchAndProbeVars[vName], size=(nWidth,-1), validator=RandBlkSzValidator())
        col_span = 1
        gbs.Add( parent.control[vName],
                 (row,1), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

        b = wx.Button(parent, -1, "Calc min block")
        parent.Bind(wx.EVT_BUTTON, self.CalcMinBlock, b)
        gbs.Add( b, (row,1+col_span), (1,5), flag=wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL)


        row += 2
        vName = 'Enable No-signal trials'
        parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
        parent.control[vName].SetValue(catchAndProbeVars[vName])
        if self.frame.FORCED_CHOICE:
            # print 'no-sig not allowed, clear it' # does not "take"
            parent.control[vName].SetValue(False)
            parent.control[vName].Disable() # FOR 2AFC
            catchAndProbeVars[vName] = False
        col_span = 1 # 2 makes ":" col wide
        gbs.Add( parent.control[vName],
                 (row,0), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        vName = 'No-sig ratio no-sig'
        gbs.Add( wx.StaticText(parent, -1, 'No-signal to signal ratio'),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % catchAndProbeVars[vName], size=(nWidth,-1))
        if self.frame.FORCED_CHOICE:
            parent.control[vName].Disable() # FOR 2AFC
        gbs.Add( parent.control[vName],
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        gbs.Add( wx.StaticText(parent, -1, ':'),
                 (row,2), (1,1), wx.ALIGN_CENTER|wx.ALL)

        vName = 'No-sig ratio sig'
        parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % catchAndProbeVars[vName], size=(nWidth,-1))
        if self.frame.FORCED_CHOICE:
            parent.control[vName].Disable() # FOR 2AFC
        gbs.Add( parent.control[vName],
                 (row,3), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        
        row += 2
        vName = 'Enable probe trials'
        parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
        parent.control[vName].SetValue(catchAndProbeVars[vName])
        col_span = 1
        gbs.Add( parent.control[vName],
                 (row,0), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        vName = 'Treat probes as no-signals'
        if catchAndProbeVars.has_key(vName):
            parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
            parent.control[vName].SetValue(catchAndProbeVars[vName])
            col_span = 2
            gbs.Add( parent.control[vName],
                     (row,0), (1,col_span), wx.ALIGN_RIGHT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

        vName = 'Probe ratio probe'
        gbs.Add( wx.StaticText(parent, -1, 'Probe to signal ratio'),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % catchAndProbeVars[vName], size=(nWidth,-1))
        gbs.Add( parent.control[vName],
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        gbs.Add( wx.StaticText(parent, -1, ':'),
                 (row,2), (1,1), wx.ALIGN_CENTER|wx.ALL)

        vName = 'Probe ratio sig'
        parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % catchAndProbeVars[vName], size=(nWidth,-1))
        gbs.Add( parent.control[vName],
                 (row,3), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        vName = 'Probe intensity'
        gbs.Add( wx.StaticText(parent, -1, vName),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % catchAndProbeVars[vName], size=(nWidth,-1))
        gbs.Add( parent.control[vName],
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        gbs.Add( wx.StaticText(parent, -1, 'dB SPL'),
                 (row,2), (1,2), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)

        sizerTopRightTB.Add(gbs, 0, wx.ALIGN_LEFT|wx.ALL, border)

        vName = 'Bounce back if reach "Max number of trials"'
        if var.has_key(vName):
            gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
            # this section goes with the catch and probe vars because it is fixed (not study-dependant)
            # and because it uses a non-generic layout, but the variables are stored in "var"
            # instead of "catchAndProbeVars"
            row = 0
            parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
            parent.control[vName].SetValue(var[vName])
            col_span = 4
            gbs.Add( parent.control[vName],
                     (row,0), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

            vName = 'Bounce back if miss'
            gbs.Add( wx.StaticText(parent, -1, vName),
                     (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
            gbs.Add( parent.control[vName],
                     (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            #row += 1
            vName = 'trials in a row'
            parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
            parent.control[vName].SetValue(var[vName])
            col_span = 1
            gbs.Add( parent.control[vName],
                     (row,2), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            #row += 1
            vName = 'signals in a row'
            parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
            parent.control[vName].SetValue(var[vName])
            col_span = 1
            gbs.Add( parent.control[vName],
                     (row,3), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            row += 1
            vName = 'Minimum bounce backs before abort'
            col_span = 3
            gbs.Add( wx.StaticText(parent, -1, vName),
                     (row,0), (1,col_span), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
            gbs.Add( parent.control[vName],
                     (row,col_span), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

            row += 1
            vName = 'Allow final bounce back after'
            col_span = 3
            gbs.Add( wx.StaticText(parent, -1, vName),
                     (row,0), (1,col_span), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
            gbs.Add( parent.control[vName],
                     (row,col_span), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            gbs.Add( wx.StaticText(parent, -1, 'trials completed'),
                     (row,col_span+1), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)


            sizerTopRightTB.Add(gbs, 0, wx.ALIGN_LEFT|wx.ALL, border)

        sizerTopLR.Add(sizerTopRightTB, 0, wx.ALIGN_TOP|wx.ALL, border)

        sizer.Add(sizerTopLR, 0, wx.ALIGN_LEFT|wx.ALL, border)

        if var.has_key('nCorrectNoSigNeeded'):
            for group in ['', '2']:
                # 2nd group are stopping rules used when "bounced-back"
                #if group == '2':
                #    continue # TEST 1 at a time
                
                # lower section - stopping rules ---------------------------------
                gbs = wx.GridBagSizer(grid_vgap, grid_hgap)
                row = 0
                col = 0
                nWidth = 35
                static_text = 'When to pass'
                static_text2 = ''
                if group == '2':
                    static_text2 += 'If bounced back to this phase'
                box = wx.StaticBox(parent, -1, static_text)
                bsizer = wx.StaticBoxSizer(box, wx.VERTICAL)
                box2 = wx.StaticBox(parent, -1, static_text2)
                bsizer2 = wx.StaticBoxSizer(box2, wx.VERTICAL)

                if group == '':
                    self.rbStopOption = [] # store the radio controls
                    rbStopOption = self.rbStopOption
                else:
                    self.rbStopOption2 = [] # store the radio controls
                    rbStopOption = self.rbStopOption2
                    
                # option 1
                col = 0
                rb = wx.RadioButton(parent, -1, '', style=wx.RB_GROUP)
                if var['nStoppingOption'+group] == 0:
                    rb.SetValue(1)
                    if self.frame.FORCED_CHOICE:
                        # print 'option 1 not allowed, clear it' # good, only happens once.
                        rb.SetValue(0)
                        rb.Disable()
                        var['nStoppingOption'+group] = 1
                else:
                    rb.SetValue(0)
                rbStopOption.append(rb)
                gbs.Add( rb, (row,col), (1,1), wx.ALIGN_CENTER|wx.ALL)
                col += 1
                vName = 'nCorrectNoSigNeeded'+group # nXcatchCorrect
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'of the last'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                vName = 'nNoSigTrialsToCheck'+group # nYcatchCorrect
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'no-signal trials correct'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1

                col = 2
                gbs.Add( wx.StaticText(parent, -1, '--- AND ---'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1

                # 2nd part of option 1
                col = 1
                vName = 'nCorrectSigNeeded'+group # nXsignalCorrect
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'of the last'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                vName = 'nSigTrialsToCheck'+group # nYsignalCorrect
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'signal trials correct'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1

                col = 2
                gbs.Add( wx.StaticText(parent, -1, '--- AND ---'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1


                # 3rd part of option 1
                col = 1
                vName = 'nCorrectProbeNeeded'+group
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'of the last'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                vName = 'nProbeTrialsToCheck'+group
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'probe trials correct'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 2

                # option 2
                col = 0
                rb = wx.RadioButton(parent, -1, '')
                if var['nStoppingOption'+group] == 1:
                    rb.SetValue(1)
                else:
                    rb.SetValue(0)
                rbStopOption.append(rb)
                gbs.Add( rb, (row,col), (1,1), wx.ALIGN_CENTER|wx.ALL)
                col += 1
                vName = 'nCorrectTrialsNeeded'+group # nXtrialCorrect
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'of the last'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                vName = 'nTrialsToCheck'+group # nYtrialCorrect
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'trials (signal or no-signal) correct'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 2

                # option 3
                col = 0
                rb = wx.RadioButton(parent, -1, '')
                if var['nStoppingOption'+group] == 2:
                    rb.SetValue(1)
                else:
                    rb.SetValue(0)
                rbStopOption.append(rb)
                gbs.Add( rb, (row,col), (1,1), wx.ALIGN_CENTER|wx.ALL)
                col += 1
                col_span = 4 # number of columns used by: __ | of the last | __ | trials...
                gbs.Add( wx.StaticText(parent, -1, 'Pass as soon as min number of trials met'),
                         (row,col), (1,col_span), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)

                bsizer.Add(gbs, 0, wx.ALL, 10) # 4th arg is margin
                bsizer2.Add(bsizer, 0, wx.ALL, 10) # 4th arg is margin

                # Max number of trials (new section, because it is outside "When to pass" section)
                gbs = wx.GridBagSizer(grid_vgap, grid_hgap) # don't really need GrisBagSizer, but simpler to use same code
                row = 0
                col = 0
                vName = 'Max number of trials'
                gbs.Add( wx.StaticText(parent, -1, vName),
                         (row,0), (1,3), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                vName += group
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                col_span = 1
                gbs.Add( parent.control[vName],
                         (row,3), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                gbs.Add( wx.StaticText(parent, -1, 'Stop (or bounce back if enabled) if reached.'),
                         (row,4), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                bsizer2.Add(gbs, 0, wx.ALIGN_TOP|wx.ALL, border)



                # stop or bounce back if x of last y probe trials missed
                gbs = wx.GridBagSizer(grid_vgap, grid_hgap) # don't really need GrisBagSizer, but simpler to use same code
                row = 0
                col = 0

                gbs.Add( wx.StaticText(parent, -1, 'If'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                vName = 'nIncorrectProbeNeeded'+group
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'of the last'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                vName = 'nProbeToCheckForMiss'+group
                parent.control[vName] = wx.TextCtrl(parent, -1, '%d' % var[vName], size=(nWidth,-1))
                gbs.Add( parent.control[vName],
                         (row,col), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                col += 1
                gbs.Add( wx.StaticText(parent, -1, 'probe trials are missed:'),
                         (row,col), (1,1), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1

                vName = 'Stop'+group
                parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
                parent.control[vName].SetValue(var[vName])
                col_span = 2
                gbs.Add( parent.control[vName],
                         (row,1), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1

                vName = 'Bounce back'+group
                parent.control[vName] = wx.CheckBox(parent, -1, vName, size=(-1,-1))
                parent.control[vName].SetValue(var[vName])
                col_span = 2
                gbs.Add( parent.control[vName],
                         (row,1), (1,col_span), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
                row += 1

                bsizer2.Add(gbs, 0, wx.ALIGN_TOP|wx.ALL, border)


                sizerBottomLR.Add(bsizer2, 0, wx.ALIGN_TOP|wx.ALL, border)


            sizer.Add(sizerBottomLR, 0, wx.ALIGN_LEFT|wx.ALL, border)

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

    def CalcMinBlock(self, event):
        minNSig,minNCat,minNPro = CalcMinSigCatProFromDlg(self.parent.control)
        self.parent.control['Random block size'].SetValue(str(minNSig + minNCat + minNPro))

    def GetData(self):

        minNSig,minNCat,minNPro = CalcMinSigCatProFromDlg(self.parent.control)
        rand_block_size = int(self.parent.control['Random block size'].GetValue())
##        if rand_block_size % (minNSig+minNCat+minNPro) != 0:
##            # not possible(?)
##            ErrorDlg(self.parent, 'Your random block size must be zero or a multiple of the min blk size.')
##            return
        if rand_block_size != 0 and rand_block_size != minNSig+minNCat+minNPro:
            ErrorDlg(self.parent, 'Your random block size must be zero or the min blk size.')
            return

        # fill in var
        varParamsFilled = []
        for param,units,convFactor,nWidth in self.params:
            if convFactor == 'CHECKBOX':
                v = self.parent.control[param].GetValue()
            elif convFactor == 'TEXT':
                v = self.parent.control[param].GetValue()

            elif convFactor == 'STATIC_TEXT':
                continue
            elif convFactor == 'INT':
                try:
                    v = int(self.parent.control[param].GetValue())
                except KeyError:
                    print 'Error interpreting value for "%s", setting it to zero.' % param
                    v = 0
            else:
                try:
                    v = convFactor * float(self.parent.control[param].GetValue())
                except KeyError:
                    print 'Error interpreting value for "%s", setting it to zero.' % param
                    v = 0.0
            self.var[param] = v
            varParamsFilled.append(param)

        # fill in self.var not filled in above (those that don't have entries in self.params)
        if len(self.var) > len(varParamsFilled):
            for param in self.var:
                if not param in varParamsFilled:
                    if param == 'nStoppingOption' or param == 'nStoppingOption2':
                        continue # done below
                    try:
                        self.var[param] = int(self.parent.control[param].GetValue())
                    except KeyError:
                        print 'Error interpreting value for "%s", setting it to zero.' % param
                        self.var[param] = 0

        # get stopping option if applicable
        if self.var.has_key('nStoppingOption'):
            for i in range(3):
                if self.rbStopOption[i].GetValue():
                    self.var['nStoppingOption'] = i
                    break
        if self.var.has_key('nStoppingOption2'):
            for i in range(3):
                if self.rbStopOption2[i].GetValue():
                    self.var['nStoppingOption2'] = i
                    break

        # fill in catchAndProbeVars
        for param in self.catchAndProbeVars:
            if param[0:4] == 'Enab': #'CHECKBOX':
                v = self.parent.control[param].GetValue()
            else:
                try:
                    v = int(self.parent.control[param].GetValue())
                except KeyError:
                    print 'Error interpreting value for "%s", setting it to zero.' % param
                    v = 0
            self.catchAndProbeVars[param] = v
        # print 'r blk sz=',self.catchAndProbeVars['Random block size']

#toyTimer = 0 # global, in case I put toy code in its own class

class SimpleModelessDlg(wx.Dialog):
    # non-modal dialog
    # supply title, message
    def __init__(self, parent, title, message):
        style = wx.CAPTION
        wx.Dialog.__init__(self, parent, -1, title, style=style)

        sizer = wx.BoxSizer(wx.VERTICAL)
        box = wx.BoxSizer(wx.HORIZONTAL)
        # this is where I could add an icon (e.g. INFO or RED X)
        box.Add( wx.StaticText(self, -1, message), 0, wx.ALIGN_CENTER_VERTICAL|wx.ALL, 5)
        sizer.AddSizer(box, 0, wx.GROW|wx.ALIGN_CENTER_VERTICAL|wx.ALL, 5)

        self.SetSizer(sizer)
        self.SetAutoLayout(True)
        sizer.Fit(self)

class PBFrame(wx.Frame):

    def __init__(self, parent, title, frameSize):
        wx.Frame.__init__(self, parent, -1, title,
                         wx.DefaultPosition, frameSize) # width, height
        self.title = title
        self.Centre()

    def TimeToQuit(self, event):
        #WarnDlg(self, "TimeToQuit: before Close()")
        self.Close(True) # will cause OnCloseWindow() to be called
        #WarnDlg(self, "TimeToQuit: after Close()")

    def Abort(self, event):
        print 'abort'
#       int retVal = AfxMessageBox("Do you want to abort?", MB_YESNO);
        self.abort = 1
        try:
            self.outFile.write('Abort pressed\n')
        except:
            pass
        try:
            self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing
        except:
            pass

    def OKAbortDlg(self, title, s, is_error=False):
        dlg = ModelessDlg(self, title, s, ["OK", "Abort"])
        dlg.CenterOnScreen()
        if is_error:
            dlg.SetBackgroundColour(wx.RED)
        dlg.Show()
        while dlg.sButt == '':
            wx.Yield()
##            if self.bToyOn:
##                if time.clock() >= self.timeEndToy:
##                    self.TurnOffToy()
            time.sleep(0.1) # FREE UP CPU ? MAKE EVENT DRIVEN?
        dlg.Destroy()
        return dlg.sButt

    def OKAbortIECDlg(self, title, s, is_error=False):
        dlg = ModelessDlg(self, title, s, ["OK", "Meas IEC", "Abort"])
        dlg.CenterOnScreen()
        if is_error:
            dlg.SetBackgroundColour(wx.RED)
        dlg.Show()
        while dlg.sButt == '':
            wx.Yield()
##            if self.bToyOn:
##                if time.clock() >= self.timeEndToy:
##                    self.TurnOffToy()
            time.sleep(0.1) # FREE UP CPU ? MAKE EVENT DRIVEN?
        dlg.Destroy()
        return dlg.sButt

    def EndTrialDlg(self, bCorrect, s, xoffset=0, enab_force_probe=0):
        if bCorrect:
            title = "Correct"
            red = False
        else:
            title = "Incorrect"
            red = True
##        dlg = ModelessDlg(self, title, s, ["OK", "Meas IEC", "Force signal trial at probe SPL", "Abort"],
        if enab_force_probe:
            dlg = ModelessDlg(self, title, s, ["OK", "Meas IEC", "Force probe trial", "Abort"],
                              self.EndTrialDlgParams, self.EndTrialDlgVars, red)
        else:
            dlg = ModelessDlg(self, title, s, ["OK", "Meas IEC", "Abort"],
                              self.EndTrialDlgParams, self.EndTrialDlgVars, red)
        if not xoffset:
            dlg.CenterOnScreen()
        else:
            display1 = wx.Display(0) # first monitor
            usable_display_size = display1.GetClientArea() # (0, 0, 1280, 1024) - Left,Top,Width,Height
            width = usable_display_size[2]
            height = usable_display_size[3]
            left = usable_display_size[0]
            top = usable_display_size[1]
            w,h = dlg.GetSizeTuple()
            x = xoffset + width/2 + left - w/2
            if x < 0:
                # print 'x at limit'
                x = 0
            y = height/2 + top - h/2
            dlg.SetPosition((x,y))
        dlg.Show()
        while dlg.sButt == '':
            wx.Yield()
            if self.bToyOn:
                if time.clock() >= self.timeEndToy:
                    self.TurnOffToy()
            time.sleep(0.01) # FREE UP CPU ? MAKE EVENT DRIVEN?
        if self.EndTrialDlgParams:
            dlg.GetData() # will write into dlg.EndTrialDlgVars, which, because pass by ref, is our copy
        dlg.Destroy()
        return dlg.sButt

    def ToyStillActiveDlg(self, s):
        dlg = ModelessDlg(self, "Toy still active", s, ["OK", "Abort"])
        dlg.CenterOnScreen()
        dlg.Show()
        while dlg.sButt == '':
            wx.Yield()
            if self.bToyOn:
                if time.clock() >= self.timeEndToy:
                    self.TurnOffToy()
                    if self.ROBOT_SIM:
                        break
            time.sleep(0.01) # FREE UP CPU ? MAKE EVENT DRIVEN?
        dlg.Destroy()
        return dlg.sButt

    def ForcedChoiceDlg(self, xoffset=0, title='1 or 2', prompt=' ', bInFCDlg=False):
        #dlg = ModelessDlg(self, "1 or 2", prompt, ["OK", "Abort"],
        dlg = ModelessDlg(self, title, prompt, ["OK", ],
                          self.ForcedChoiceParams, self.ForcedChoiceVars)
        if self.ADULT_SUBJECT and self.dlg_mask1a:
            # print 'using 2nd monitor - center on it'
            w,h = dlg.GetSizeTuple()
            x = self.center2H - w/2
            y = self.center2V - h/2
            dlg.SetPosition((x,y))
        else:
            # print 'using 1st monitor'
            if not xoffset:
                dlg.CenterOnScreen()
            else:
                display1 = wx.Display(0) # first monitor
                usable_display_size = display1.GetClientArea() # (0, 0, 1280, 1024) - Left,Top,Width,Height
                width = usable_display_size[2]
                height = usable_display_size[3]
                left = usable_display_size[0]
                top = usable_display_size[1]
                w,h = dlg.GetSizeTuple()
                x = xoffset + width/2 + left - w/2
                if x < 0:
                    # print 'x at limit'
                    x = 0
                y = height/2 + top - h/2
                dlg.SetPosition((x,y))
        dlg.Show()
        if hasattr(self.controlPanel, 'time_bar'):
            self.controlPanel.init_bar_begin()
        while dlg.sButt == '':
            if hasattr(self.controlPanel, 'time_bar'):
                timeout = self.controlPanel.update_bar()
                if timeout:
                    # don't count trial
                    dlg.Destroy()
                    return 'Timeout'
            wx.Yield()
            if self.bToyOn:
                if time.clock() >= self.timeEndToy:
                    self.TurnOffToy()
            time.sleep(0.01) # FREE UP CPU ? MAKE EVENT DRIVEN?
            if self.abort:
                # esc was pressed
                dlg.sButt = 'Abort'
            if bInFCDlg:
                # this happends a lot # print 'lets get away from it all'
                if self.FCDlg_key_pressed:
                    #print "GOT IT:",self.FCDlg_key_pressed
                    if self.FCDlg_key_pressed == '1' or self.FCDlg_key_pressed == '2' or self.FCDlg_key_pressed == '3':
                        self.ForcedChoiceVars['Choice'] = int(self.FCDlg_key_pressed)
                        dlg.Destroy()
                        return 'OK'
        dlg.GetData(-1) # arg is return value if they enter non-number
        dlg.Destroy()
        return dlg.sButt

    def OnCloseWindow(self, event):
        # called if file->exit or click on [x]
        #WarnDlg(self, "OnCloseWindow: ")
        # if I return now, the app will not exit

        #print 'exit 1'
        if self.bDevOutputInitd and hw.devOutput == 'SoundCard':
            import pygame.mixer
            #print 'exit 2'
            pygame.mixer.stop()
            #print 'exit 3'

        #print 'exit 4'
        self.exit = 1
        #print 'exit 5'
        self.Destroy()
        #print 'exit 6'

    def CopyPhase(self, event):
        src_phase = int(event.GetEventObject().GetLabel()[-2:]) - 1 # 1st is 0 (user sees 1)
        phase = src_phase + 1
        for i in range(src_phase, phase):
            self.trainVars[i+1] = self.trainVars[i].copy()
            self.catchAndProbeVars[i+1] = self.catchAndProbeVars[i].copy()
            self.upDownVars[i+1] = self.upDownVars[i].copy()
        InfoDlg(self, 'Phase %d settings copied to phase %d.' % (src_phase+1, phase+1))

    def add_setting_if_missing(self, settings, setting, default):
        if not settings.has_key(setting):
            settings[setting] = default
            # print 'set',setting

    def OnLoadSettings(self, evt):
        wildcard = "PsychoBaby settings file (*.set)|*.set|" \
           "All files (*.*)|*.*"

        dlg = wx.FileDialog(
            self, message="Choose a file", defaultDir=self.currPath, 
            defaultFile="", wildcard=wildcard, style=wx.OPEN
            )
        if dlg.ShowModal() != wx.ID_OK:
            return
        path = dlg.GetPath()
        self.currPath = os.path.split(path)[0] # must store ONLY path
        self.settings_file = path
        try:
            self.LoadSettings(path)
            # self.SetTitle('%s - %s' % (os.path.basename(path), self.title)) # this info can bias the user
            self.SetTitle(self.title)
        except:
            ErrorDlg(self, 'Error understanding setings file.')

    def gen_multi_tone(self, nPoints, freqs, mult):
        result = numpy.zeros(dtype=numpy.float32, shape=(nPoints,))
        for f in freqs:
            slope = 2.0 * numpy.pi * f / self.srate
            result += numpy.sin(numpy.arange(0.0, slope*(nPoints+1), slope, dtype=numpy.float32)[:nPoints])
        nfreqs = len(freqs)
        mult /= float(nfreqs)
        return result*mult

    def ConfigPhase(self, event):
        print 'ConfigPhase() v2'
        # button = event.GetEventObject()
        phase = int(event.GetEventObject().GetLabel()[-1:]) - 1 # 1st is 0 (user sees 1)
        if self.controlPanel.phaseType[phase]['TRAIN'].GetValue():
            dlg = SettingsPhase(self, 'Training (non-adaptive) settings',
                                self.trainVars[phase], self.train1Params, self.catchAndProbeVars[phase], phase) # passed by ref!
        elif self.controlPanel.phaseType[phase]['UPDOWN'].GetValue():
            dlg = SettingsPhase(self, 'Up/Down settings',
                                self.upDownVars[phase], self.upDownParams, self.catchAndProbeVars[phase], phase) # passed by ref!
        else:
            WarnDlg(self, 'Nothing to config for Skip')
            return

        val = dlg.ShowModal()
        if val == wx.ID_OK:
            dlg.GetData() # will write into dlg.filenameVars, which, because pass by ref, is our copy
        dlg.Destroy()
        try:
            self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing
        except:
            pass


###################

    def initPA5(self):
        return tdt_sys3.initPA5(self, hw.strConnect)

    def MovieFin(self, event):
        print 'movie fin'
        if time.clock() < self.tForceToy:
            self.controlPanel.mc.Seek(0)
            event.Veto()
        
    def TurnOffToy(self, evt=0):
        if len(self.toys.strDevice):
            self.toys.TurnOff()
        self.bToyOn = False

    def TurnOnAToy(self, toy_dur): # toy_dur in seconds
##        if self.bToyOn:
##            # toy already on, probably manually forced
##            return
        self.TurnOnToy(toy_dur)

    def TurnOnToy(self, toy_dur, ntoy=-1):  # toy_dur in seconds
        if len(self.toys.strDevice):
            if hw.TOY_CONTROLLER == 'ONTRAK ADU208':
                self.toys.TurnOn(self.EndTrialDlgVars['Enable toys'], self.EndTrialDlgVars['Enable DVD'], nToy=ntoy)
            else:
                self.toys.TurnOn(toy_dur=toy_dur)
        self.bToyOn = True
        self.timeEndToy = time.clock() + toy_dur # when toy will get turned off
##        global toyTimer
##        toyTimer.Start(int(toy_dur * 1000), wx.TIMER_ONE_SHOT)
        if 0: # ENAB_MOVIE and self.nMovies > 0:
            nMovie = random.randint(0, self.nMovies-1)
            if not self.controlPanel.mc.Load(self.moviePaths[nMovie]):
                print 'unable to load movie'
            else:
                self.controlPanel.mc.SetBestFittingSize()
                self.controlPanel.mc.Play()

    def CalcRecentHitRate(self, N, nTrialType, treat_probes_as_no_signals=False):
        # updated to be like calc_recent_hit_rate2() 2/24/2012
        # return str
        nPhase = self.nPhaseAll[self.nTrialIndexAll]
        nCorrect = 0
        nTrials  = 0    # n trials we have looked at
        for i in range(self.nTrialIndexAll, -1, -1):
            if self.nPhaseAll[i] != nPhase:
                continue
            if self.nTrialTypeAll[i] != nTrialType:
                if self.nTrialNumAll[i] == 0: # 1st is 0
                    break
                continue
            nTrials += 1
            if self.bCorrectAll[i]:
                nCorrect += 1
            if nTrials >= N:
                break       # we've seen enough
            if self.nTrialNumAll[i] == 0: # 1st is 0
                break
        if nTrials == N:
            if nTrialType == pb.NO_SIG or nTrialType == pb.PROBE and treat_probes_as_no_signals:
                # reverse: from hit rate to false alarm rate
                return "%.0f %%" % ( 100.0 - 100.0*float(nCorrect)/N )
            else:
                return "%.0f %%" % ( 100.0*float(nCorrect)/N )
        else:
            return ''

    def AdjStepSizeUsingPEST(self, newDir, var):
        #done = self.AdjStepSizeUsingPEST(-1, var)
        self.nStepNum += 1
        if self.nDir == -newDir:
            # reversal
            self.nStepsInCurrDir = 0
            self.nReversals += 1
            self.nLastReversalStepNum = self.nStepNum
            if self.nReversals > var['Ignore']:
                # estimate threshold
                i = self.nReversals - var['Ignore'] - 1
                self.fThrEst[i] = (self.fLastLevelNCorrect + self.fLastLevelMMiss) / 2.0
                #print 'thr est = %.1f,  last level correct = %.1f, last level miss = %.1f' % (
                #    self.fThrEst[i], self.fLastLevelNCorrect, self.fLastLevelMMiss)
            if self.nReversals >= var['Use'] + var['Ignore']:
                # we are done
                return True
        self.nDir = newDir
        self.nStepsInCurrDir += 1
        if self.nStepsInCurrDir == 1:
            # 1st step in current direction
            if self.nReversals:
                # half the step size
                self.fSignalStepSize *= 0.5
                if not self.bUpDownIsIntensity:
                    self.fSignalStepSize = int(self.fSignalStepSize)
                if self.fSignalStepSize < var['Min step size']:
                    self.fSignalStepSize = var['Min step size']
        elif self.nStepsInCurrDir == 2:
            # no change in step size
            pass
        elif self.nStepsInCurrDir == 3:
            if self.nLastStepNumDoubled and self.nLastReversalStepNum and (
                self.nLastReversalStepNum-1 == self.nLastStepNumDoubled):
                # no change in step size
                pass
            else:
                # double this step
                self.fSignalStepSize *= 2.0
                self.nLastStepNumDoubled = self.nStepNum
                if not self.bUpDownIsIntensity:
                    self.fSignalStepSize = int(self.fSignalStepSize)
                if self.fSignalStepSize > var['Max step size']:
                    self.fSignalStepSize = var['Max step size']
        else:
            # 4th and subsequent step in current dir
            # double this step
            self.fSignalStepSize *= 2.0
            self.nLastStepNumDoubled = self.nStepNum
            if not self.bUpDownIsIntensity:
                self.fSignalStepSize = int(self.fSignalStepSize)
            if self.fSignalStepSize > var['Max step size']:
                self.fSignalStepSize = var['Max step size']
        return False

# ------------ begin random routines ------------

    def InitRandTrials(self, nRandBlkSz, bEnabNoSig, bEnabProbe, nCatchRatio_catch, nCatchRatio_sig, nProbeRatio_probe, nProbeRatio_sig):
        # works for all combinations of catch, probe enables, even if both disabled
        # compute self.nBins (the number of possible random numbers)
        # first calc min num of sig, cat, pro (LOGIC DUPLICATED IN calc_min_blk())
        nSig,nCat,nPro = CalcMinSigCatPro(bEnabNoSig, bEnabProbe, nCatchRatio_catch, nCatchRatio_sig, nProbeRatio_probe, nProbeRatio_sig)
        self.nBins = nSig + nCat + nPro;                             # good even if not both

        if nRandBlkSz != 0:
            # blocking
            iMult = nRandBlkSz / self.nBins
            nSig *= iMult;
            nCat *= iMult;
            nPro *= iMult;
            self.nBins *= iMult;

        self.nCatchThresh = nCat - 1;    # if catch trials not enabled, then nCatchThresh will be -1
        self.nProbeThresh = nPro + nCat - 1;

        self.bRanDon = numpy.zeros(shape=(self.nBins,), dtype=numpy.bool_)
        self.nRanDone = 0
            

    def IsTrainComplete(self, var, catchAndProbeVars, nTrial, nPhase):
        # just returns a boolean. no side-effects.
        if not self.nBouncedBackToThisPhase[nPhase]:
            # normal group of stopping rules
            group = ''
        else:
            # second group of stopping rules
            group = '2'
        if var['nStoppingOption'+group] == 0:
            # option 1
            nCorrect = 0
            nTrials = 0     # n trials we have looked at
            i = self.nTrialIndexAll-1
            while(i >= 0):
                if self.nPhaseAll[i] != nPhase:
                    i -= 1
                    continue
                if self.nTrialTypeAll[i] != pb.NO_SIG:
                    if self.nTrialNumAll[i] == 0: # 1st is 0
                        break
                    i -= 1
                    continue
                nTrials += 1
                if self.bCorrectAll[i]:
                    nCorrect += 1
                if nTrials >= var['nNoSigTrialsToCheck'+group]:
                    break # we've seen enough
                if self.nTrialNumAll[i] == 0:
                    break # no more trials
                i -= 1
            if nCorrect < var['nCorrectNoSigNeeded'+group]:
                return False
            
            nCorrect = 0
            nTrials = 0     # n trials we have looked at
            i = self.nTrialIndexAll-1
            while(i >= 0):
                if self.nPhaseAll[i] != nPhase:
                    i -= 1
                    continue
                if self.nTrialTypeAll[i] != pb.SIGNAL:
                    if self.nTrialNumAll[i] == 0: # 1st is 0
                        break
                    i -= 1
                    continue
                nTrials += 1
                if self.bCorrectAll[i]:
                    nCorrect += 1
                if nTrials >= var['nSigTrialsToCheck'+group]:
                    break
                if self.nTrialNumAll[i] == 0:
                    break # no more trials
                i -= 1
            if nCorrect < var['nCorrectSigNeeded'+group]:
                return False

            # check probe trials (added 1/21/2015)
            if var['nCorrectProbeNeeded'+group] > 0:
                nCorrect = 0
                nTrials = 0     # n trials we have looked at
                i = self.nTrialIndexAll-1
                while(i >= 0):
                    if self.nPhaseAll[i] != nPhase:
                        i -= 1
                        continue
                    if self.nTrialTypeAll[i] != pb.PROBE:
                        if self.nTrialNumAll[i] == 0: # 1st is 0
                            break
                        i -= 1
                        continue
                    nTrials += 1
                    if self.bCorrectAll[i]:
                        nCorrect += 1
                    if nTrials >= var['nProbeTrialsToCheck'+group]:
                        break # we've seen enough
                    if self.nTrialNumAll[i] == 0:
                        break # no more trials
                    i -= 1
                if nCorrect < var['nCorrectProbeNeeded'+group]:
                    return False

        elif var['nStoppingOption'+group] == 1:
            # option 2
            nCorrect = 0
            nTrials = 0     # n trials we have looked at
            i = self.nTrialIndexAll-1
            while(i >= 0):
                if self.nPhaseAll[i] != nPhase:
                    i -= 1
                    continue
                if self.nTrialTypeAll[i] == pb.PROBE:
                    if self.nTrialNumAll[i] == 0: # 1st is 0
                        break
                    i -= 1
                    continue
                nTrials += 1
            
                if self.bCorrectAll[i]:
                    nCorrect += 1
                if nTrials >= var['nTrialsToCheck'+group]:
                    break
                if self.nTrialNumAll[i] == 0:
                    break # no more trials
                i -= 1
            if nCorrect < var['nCorrectTrialsNeeded'+group]:
                return False


        if self.nSignal[nPhase] < 1:
            self.outFile.write('Would have been done with this phase, but we have not had a signal trial yet.\n')
            return False

        if self.nSignal[nPhase] < var['Min number of signal trials']:
            if var['nStoppingOption'+group] != 2:
                self.outFile.write('Would have been done with this phase, but we have not reached the "Min number of signal trials" (%s)\n' % var['Min number of signal trials'])
            return False
        if catchAndProbeVars['Enable No-signal trials'] and self.nCatch[nPhase] < var['Min number of no-signal trials']:
            if var['nStoppingOption'+group] != 2:
                self.outFile.write('Would have been done with this phase, but we have not reached the "Min number of no-signal trials" (%s)\n' % var['Min number of no-signal trials'])
            return False
        if catchAndProbeVars['Enable probe trials'] and self.nProbe[nPhase] < var['Min number of probe trials']:
            if var['nStoppingOption'+group] != 2:
                self.outFile.write('Would have been done with this phase, but we have not reached the "Min number of probe trials" (%s)\n' % var['Min number of probe trials'])
            return False

        return True

    def SelectTrialType(self, mode, var, catchAndProbeVars, nTrial, nPhase):
        # returns (bDone, nRand)
        self.strTrialType = ""
        if mode == 'TRAIN':
            if self.IsTrainComplete(var, catchAndProbeVars, nTrial, nPhase): # just returns a boolean. no side-effects.
                return (True, 0)

        # ======= see if we are done =========



        #  IF nIncorrectProbeNeeded OF THE LAST nProbeToCheckForMiss PROBE TRIALS ARE MISSED:
        #     [ ] Stop [ ] bounce back.
        if not self.nBouncedBackToThisPhase[nPhase]:
            # normal group of stopping rules
            group = ''
        else:
            # second group of stopping rules
            group = '2'
        if mode == 'TRAIN' and (var['Stop'+group] or var['Bounce back'+group]):
            # see if we should STOP or BOUNCE-BACK
            nMissed = 0     # n probe trials we have missed
            nTrials = 0     # n probe trials we have looked at
            i = self.nTrialIndexAll-1
            while(i >= 0):
                if self.nPhaseAll[i] != nPhase:
                    i -= 1
                    continue
                if self.nTrialTypeAll[i] != pb.PROBE:
                    if self.nTrialNumAll[i] == 0: # 1st is 0
                        break
                    i -= 1
                    continue
                nTrials += 1
                if not self.bCorrectAll[i]:
                    nMissed += 1
                if nTrials >= var['nProbeToCheckForMiss'+group]:
                    break # we've seen enough
                if self.nTrialNumAll[i] == 0:
                    break # no more trials
                i -= 1
            if nTrials >= var['nProbeToCheckForMiss'+group] and nMissed >= var['nIncorrectProbeNeeded'+group]:
                # STOP or BOUNCE-BACK. At least one of those is enabled due to enclosing IF above.
                what_happened = '%d of the last %d probes were missed. (setting is %d/%d)' % (
                    nMissed, nTrials, var['nIncorrectProbeNeeded'+group], var['nProbeToCheckForMiss'+group], )
                if var['Stop'+group]:
                    # STOP
                    self.abort = 1
                    msg = '%s Will now abort.' % what_happened
                elif var['Bounce back'+group]:
                    # bounce back
                    self.abort = 5 # MISSED X TRIALS/SIGNALS IN A ROW (may do >1 BB)
                    msg = '%s. We should bounce back now.' % what_happened
                self.outFile.write(msg+'\n')
                if not self.SIMULATION:
                    MsgDlg(self, 'Too many missed probes', msg)
                return (True,0)

        if not self.nBouncedBackToThisPhase[nPhase]:
            nMaxTrials = var['Max number of trials']
        else:
            nMaxTrials = var['Max number of trials2']
        if nMaxTrials > 0:
            # there is a limit
            if nTrial+self.nTrialsSoFar[nPhase] >= nMaxTrials:
                # Reached 'Max number of trials' setting
                vName = 'Bounce back if reach "Max number of trials"'
                if var.has_key(vName) and var[vName]:
                    self.abort = 4 # indicate that we reached max
                    self.outFile.write('Reached "Max number of trials" setting. We should bounce back now.\n')
                else:
##                    ret = OKCancelDlg(self, 'Reached Max Trials',
##                                      'Reached Max Trials setting. Click OK to continue to next phase, or CANCEL to abort.')
##                    if ret == wx.ID_CANCEL:
##                        self.abort = 1
                    if self.SIMULATION:
                        self.abort = 1
                    else:
                        self.outFile.write('Reached "Max number of trials" setting. Will now abort.\n')
                        MsgDlg(self, 'Reached Max Trials',
                                     'Reached Max Trials setting. Will now abort.')
                        self.abort = 1
                return (True,0)

        # ============ determine trial type (signal, catch, or probe) ============
        # print 'self.nBins=',self.nBins
        if self.nBins <= 1:
            nRand = 0
        else:
            nRand = random.randint(0, self.nBins-1) # returns >= 0 and <=nBins-1
        if catchAndProbeVars['Random block size'] > 0:
            if self.nRanDone >= self.nBins:
                self.bRanDon[:] = False
                self.nRanDone = 0
            #
            while self.bRanDon[nRand]:
                nRand += 1
                if nRand >= self.nBins:
                    nRand = 0
            self.bRanDon[nRand] = True
            self.nRanDone += 1
        # nRand is good
        self.bIsProbe = 0
        if nRand <= self.nCatchThresh:
            # catch (no-signal)
            self.strTrialType = "no-sig"
            self.fIntenCh1 = pb.NO_SIG_INTENSITY
            self.nTrialType[nTrial] = pb.NO_SIG
            self.nCatch[nPhase] += 1
        elif nRand <= self.nProbeThresh:
            # probe
            # NOTE: if any probe code changes between here and routine exit, update PB-Horn bForceProbeTrial code
            self.strTrialType = "probe"
            self.fIntenCh1 = catchAndProbeVars['Probe intensity']
            self.nTrialType[nTrial] = pb.PROBE
            self.bIsProbe = 1
            self.nProbe[nPhase] += 1
        else:
            # signal
            self.strTrialType = "signal"
            if mode == 'TRAIN':
                self.fIntenCh1 = var['Intensity']
            elif mode == 'UPDOWN':
                self.fIntenCh1 = self.fSignalInten
            try:
                self.nTrialType[nTrial] = pb.SIGNAL
            except:
                ErrorDlg(self, 'SOFTWARE ERROR, must stop early. Tell Brandon that location is SelectTrialType:001')
                return (True,0)
            self.nSignal[nPhase] += 1
            self.nSignal_current += 1
        self.nTrialTypeAll[self.nTrialIndexAll] = self.nTrialType[nTrial]
        return (False, nRand)

# ------------ end random routines ------------

    # this could be moved into C/DLL if needed
    def WaitForUser(self, tBegin, tDelay, tIgnore=0, time_to_hide_dlg_operator=0):
        self.tPlusKey = 0 # time that "+" pressed
        tEnd = tBegin + tDelay
        tEndIgnore = tBegin + tIgnore
        while(1):
            if time_to_hide_dlg_operator:
                if time.clock() >= time_to_hide_dlg_operator:
                    self.dlg_operator.Show(False)
                    time_to_hide_dlg_operator = 0
            wx.Yield()
            bIgnoreUser = 0
            if tIgnore:
                theTime = time.clock()
                if theTime < tEndIgnore:
                    bIgnoreUser = 1
            if not bIgnoreUser and self.tPlusKey > tBegin:
                if time_to_hide_dlg_operator:
                    self.dlg_operator.Show(False)
                return self.tPlusKey
            else:
                self.tPlusKey = 0
            if self.abort:
                if time_to_hide_dlg_operator:
                    self.dlg_operator.Show(False)
                return -1
            if time.clock() >= tEnd:
                if time_to_hide_dlg_operator:
                    self.dlg_operator.Show(False)
                return -1
            time.sleep(0.02) # FREE UP CPU ? MAKE EVENT DRIVEN?


    def SkipToNextPhase(self, event):
#       int retVal = AfxMessageBox("Do you want to skip to the next phase?", MB_YESNO);
        self.abort = 2
        try:
            self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing
        except:
            pass
        try:
            self.outFile.write('SkipToNextPhase pressed\n')
        except:
            pass

    def SkipToTesting(self, event):
#       int retVal = AfxMessageBox("Do you want to skip to the testing phase?", MB_YESNO);
        self.abort = 3
        try:
            self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing
        except:
            pass
        try:
            self.outFile.write('SkipToTesting pressed\n')
        except:
            pass

if __name__ == "__main__":
    import doctest
    doctest.testmod()

