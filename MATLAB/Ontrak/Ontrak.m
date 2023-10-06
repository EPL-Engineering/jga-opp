classdef Ontrak
   % Ontrak: interface to control ADU208
   % KEH 05/01/2023
   %

   properties (SetAccess = private)
      deviceHandle = [];
      debug = false;
      debugValue = 0;
   end

   methods
      function obj = Ontrak(debugMode)
         if nargin < 1
            debugMode = false;
         end
         obj.debug = debugMode;

         % Ontrak: Construct an instance of this class

         % --- Load library ---
         % If this fails, throw an exception: this is an install or path
         % problem that needs to be straightened out before continuing.
         try
            if not(libisloaded('AduHid64'))
               loadlibrary('AduHid64', 'AduHidMatlab.h');
            end
         catch ex
            error('Failed to load Ontrak ADU control library (AduHid64.dll).')
         end
      end

      %--------------------------------------------------------------------
      function obj = Initialize(obj)
         % Initialize: Connect to device, clear output

         % Obtain handle to device
         try
            obj.deviceHandle = calllib('AduHid64', 'OpenAduDevice', 1000); % if only one ADU is connected
         catch
            error('Failed to initialize Ontrak ADU device. Make sure it is connected and try again.');
         end

%          obj.TurnOff();
      end

      %--------------------------------------------------------------------
      function obj = TurnOn(obj, useToy1, useToy2, useMotor)
         bitMask = 0;
         if useToy1, bitMask = bitMask + 1; end
         if useToy2, bitMask = bitMask + 2; end
         if useMotor, bitMask = bitMask + 4; end

         obj.WriteAduDevice(bitMask);
%          fprintf('write bitmask = %d\n', bitMask);

         nwait = 50;
         ntries = 0;
%          tic
         while ntries < nwait
             ntries = ntries + 1;
%              pause(2);
             readMask = obj.ReadAduDevice();
%              fprintf('%d: %d\n', ntries, readMask);
             if readMask == bitMask
                 break;
             end
         end
%          disp(toc);
      end

      %--------------------------------------------------------------------
      function obj = TurnOff(obj)
         obj.WriteAduDevice(0);
      end

      %--------------------------------------------------------------------
      function [toy1, toy2, motor] = GetState(obj)
         bitMask = obj.ReadAduDevice();
%          fprintf('read bitmask = %d\n', bitMask);
         toy1 = bitand(bitMask, 1) > 0;
         toy2 = bitand(bitMask, 2) > 0;
         motor = bitand(bitMask, 4) > 0;
      end

      %--------------------------------------------------------------------
      function obj = Close(obj)
         if ~isempty(obj.deviceHandle)
            calllib('AduHid64', 'CloseAduDevice', obj.deviceHandle);
            obj.deviceHandle = [];
            disp('Closed Ontrak library');
         end
      end

      %--------------------------------------------------------------------
      function delete(obj)
         obj.Close(); % just to be sure
         unloadlibrary('AduHid64');
         disp('Unloaded Ontrak library');
      end
   end

   %-----------------------------------------------------------------------
   methods (Access = private)
      function obj = WriteAduDevice(obj, val)
         % check that device has been initialized
         if isempty(obj.deviceHandle)
            error('Cannot write to Ontrak ADU. Device is not initialized.');
         end

         if obj.debug
            obj.debugValue = val;
            fprintf('Ontrak debug value = %d\n', val);
            return;
         end

         data = int8(sprintf('MK%d', val));

         result = calllib('AduHid64', 'WriteAduDevice', obj.deviceHandle, ...
            libpointer('voidPtr', data), length(data), ...
            libpointer('voidPtr',zeros(1,1)), 500);

         % check result of write
         if result == 0
            error('Error writing to Ontrak ADU device.');
         end
      end

      %--------------------------------------------------------------------
      function val = ReadAduDevice(obj)
         % check that device has been initialized
         if isempty(obj.deviceHandle)
            error('Cannot read from Ontrak ADU. Device is not initialized.');
         end

         if obj.debug
            val = obj.debugValue;
            return;
         end

         % send read request
         data = int8('PK');

         result = calllib('AduHid64', 'WriteAduDevice', obj.deviceHandle, ...
            data, 8, ...
            libpointer('voidPtr',zeros(1,1)), 500);

         % check result of write
         if result == 0
            error('Error readin from Ontrak ADU device.');
         end

         % read result
         ptrResult = libpointer('int8Ptr', zeros(1,8));

         result = calllib('AduHid64', 'ReadAduDevice', obj.deviceHandle, ...
            ptrResult, 8, ...
            libpointer('voidPtr',zeros(1,1)), 500);

         % check result of write
         if result == 0
            error('Error readin from Ontrak ADU device.');
         end

         x = char(ptrResult.value);
         val = str2double(x);       
      end

   end

end