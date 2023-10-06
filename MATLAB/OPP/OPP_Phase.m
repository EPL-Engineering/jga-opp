classdef OPP_Phase
   % OPP_PHASE -- encapsulates properties and methods for executing an
   % OPP phase.

   properties (SetAccess = private, GetAccess = public)
      Number
      StimValue
      Data
      Started = false;
      TrialResult
   end

   properties (SetAccess = private)
      settings
      passIf
      direction
      trialNum
      lastN_nosignal
      lastN_signal
   end

   methods
      function obj = OPP_Phase()
      end

      %--------------------------------------------------------------------
      function obj = Initialize(obj, number, value, settings, direction)
         obj.Number = number;
         obj.StimValue = value;
         obj.settings = settings;
         obj.direction = direction;

         if isequal(direction, 'forward')
            obj.passIf = obj.settings.passIf;
         else
            obj.passIf = obj.settings.passBounceIf;
         end

         obj.Started = false;
         obj.trialNum = 1;
         obj.lastN_nosignal = -1;
         obj.lastN_signal = -1;

         obj.Data = obj.CreateDataStructure(number, value, direction);
         obj = obj.AddBlock();
      end

      %--------------------------------------------------------------------
      function obj = UpdateSettings(obj, settings)
         obj.settings = settings;

         if isequal(obj.direction, 'forward')
            obj.passIf = obj.settings.passIf;
         else
            obj.passIf = obj.settings.passBounceIf;
         end

         obj.Data.hasSignal = obj.Data.hasSignal(1:length(obj.Data.correct));
         obj = obj.AddBlock();
      end

      %--------------------------------------------------------------------
      function obj = SetWaveformName(obj, destination, name)
         obj.Data.(destination) = name;
      end

      %--------------------------------------------------------------------
      function value = CurrentTrialHasSignal(obj)
         value = obj.Data.hasSignal(obj.trialNum);
      end

      %--------------------------------------------------------------------
      function value = MustRespond(obj)
         value = obj.settings.rewardRequiresResponse;
      end

      %--------------------------------------------------------------------
      function [obj, result, message] = ProcessResult(obj, haveResponse, responseTime, reward)

         obj.Started = true;
         
         if obj.CurrentTrialHasSignal()
            if haveResponse
               obj.TrialResult = 'hit';
            else
               obj.TrialResult = 'miss';
            end
         else
            if haveResponse
               obj.TrialResult = 'false alarm';
            else
               obj.TrialResult = 'withhold';
            end
         end

         isCorrect = obj.CurrentTrialHasSignal() && haveResponse;
         isCorrect = isCorrect || (~obj.CurrentTrialHasSignal() && ~haveResponse);
         if obj.trialNum == 1
            obj.Data.response = haveResponse;
            obj.Data.responseTime = responseTime;
            obj.Data.correct = isCorrect;
            obj.Data.reward = {reward};
         else
            obj.Data.response(end+1) = haveResponse;
            obj.Data.responseTime(end+1) = responseTime;
            obj.Data.correct(end+1) = isCorrect;
            obj.Data.reward{end+1} = reward;
         end

         if length(obj.Data.hasSignal) == obj.trialNum
            obj = obj.AddBlock();
         end

         ifilt = find(obj.Data.hasSignal(1:obj.trialNum) == 1);
         if ~isempty(ifilt)
            obj.Data.hitRate = sum(obj.Data.correct(ifilt)) / length(ifilt);
         end

         ifilt = find(obj.Data.hasSignal(1:obj.trialNum) == 0);
         if ~isempty(ifilt)
            obj.Data.falseAlarmRate = sum(~obj.Data.correct(ifilt)) / length(ifilt);
         end

         lastN = find(~obj.Data.hasSignal(1:obj.trialNum), obj.passIf.outOfNumNoSignal, 'last');
         if length(lastN) >= obj.passIf.outOfNumNoSignal
            obj.lastN_nosignal = sum(obj.Data.correct(lastN));
         end
         nosigCritMet = obj.lastN_nosignal >= obj.passIf.numNoSignalCorrect;

         lastN = find(obj.Data.hasSignal(1:obj.trialNum), obj.passIf.outOfNumSignal, 'last');
         if length(lastN) >= obj.passIf.outOfNumSignal
            obj.lastN_signal = sum(obj.Data.correct(lastN));
         end
         sigCritMet = obj.lastN_signal >= obj.passIf.numSignalCorrect;

         trialsCritMet = false;
         if length(obj.Data.correct) >= obj.passIf.outOfNumTrials
            c = flip(obj.Data.correct);
            n = sum(c(1:obj.passIf.outOfNumTrials));
            trialsCritMet = n >= obj.passIf.numTrialsCorrect;
         end

         result = 'continue';
         message = '';

         if isequal(obj.passIf.criterion, 'both') && nosigCritMet && sigCritMet
            % --- PASS CRITERION #1: consecutive hits and false alarms ---
            result = 'passed';
            message = sprintf('Satisfied hit (%d/%d) and false alarm (%d/%d) criteria', ...
               obj.passIf.numSignalCorrect, obj.passIf.outOfNumSignal, ...
               obj.passIf.numNoSignalCorrect, obj.passIf.outOfNumNoSignal ...
               );
         elseif isequal(obj.passIf.criterion, 'trials') && trialsCritMet
            % --- PASS CRITERION #2: consecutive correct (regardless of trial type) ---
            result = 'passed';
            message = sprintf('Satisfied correct response (%d/%d) criterion', ...
               obj.passIf.numTrialsCorrect, obj.passIf.outOfNumTrials ...
               );
         elseif obj.trialNum >= obj.passIf.maxNumTrials && obj.settings.bounceBack.ifMaxNumTrials
            % --- FAIL CRITERION #1: max number of trials ---
            result = 'failed';
            message = sprintf('Reached maximum number of trials allowed (%d)', obj.passIf.maxNumTrials);
         else
            if obj.settings.bounceBack.ifConsecutiveMiss
            % --- FAIL CRITERION #2: consecutive misses ---
               if isequal(obj.settings.bounceBack.consecutiveMissWhat, 'signals')
                  % --- FAIL CRITERION #2a: consecutive misses on signal trials ---
                  lastN = find(obj.Data.hasSignal(1:obj.trialNum), obj.settings.bounceBack.numConsecutiveMiss, 'last');
                  if length(lastN) >= obj.settings.bounceBack.numConsecutiveMiss && ~any(obj.Data.correct(lastN))
                     result = 'failed';
                     message = sprintf('Missed %d signals in a row', obj.settings.bounceBack.numConsecutiveMiss);
                  end
               elseif isequal(obj.settings.bounceBack.consecutiveMissWhat, 'trials')
                  % --- FAIL CRITERION #2b: consecutive misses on trials of any kind ---
                  if length(obj.Data.correct) >= obj.settings.bounceBack.numConsecutiveMiss
                     c = flip(obj.Data.correct);
                     if ~any(c(1:obj.settings.bounceBack.numConsecutiveMiss))
                        result = 'failed';
                        message = sprintf('Missed %d trials in a row', obj.settings.bounceBack.numConsecutiveMiss);
                     end
                  end
               end
            end
         end

         if ~isequal(result, 'continue')
            obj.Data.result = [result ': ' message];
         end
         obj.trialNum = obj.trialNum + 1;

      end

      %--------------------------------------------------------------------
      function D = GetTableData(obj)
         D = cell(3, 2);
         if obj.Data.hitRate > -1
            D{1,1} = sprintf('%d %%', round(100*obj.Data.hitRate));
         end
         if obj.lastN_signal > -1
            D{1,2} = sprintf('%d / %d', obj.lastN_signal, obj.passIf.outOfNumSignal);
         end
         if obj.Data.falseAlarmRate > -1
            D{2,1} = sprintf('%d %%', round(100*obj.Data.falseAlarmRate));
         end
         if obj.lastN_nosignal > -1
            D{2,2} = sprintf('%d / %d', obj.lastN_nosignal, obj.passIf.outOfNumNoSignal);
         end
         D{3,1} = sprintf('%d', length(obj.Data.correct));
      end

      %--------------------------------------------------------------------
      function N = GetNumberOfTrials(obj)
         N = length(obj.Data.response);
      end

   end

   %--------------------------------------------------------------------------
   % PRIVATE METHODS
   %--------------------------------------------------------------------------
   methods (Access = private)

      function D = CreateDataStructure(~, number, value, direction)
         D = struct(...
            'phaseNumber', number, ...
            'phaseValue', value, ...
            'direction', direction, ...
            'background', '', ...
            'background2', '', ...
            'signal', '', ...
            'hasSignal', [], ...
            'response', [], ...
            'reward', [], ...
            'responseTime', [], ...
            'correct', [], ...
            'hitRate', -1, ...
            'falseAlarmRate', -1, ...
            'result', '' ...
            );
      end

      %--------------------------------------------------------------------------
      function obj = AddBlock(obj)
         hasSignal = [ones(1, obj.settings.numSignal) zeros(1, obj.settings.numNoSignal)];
         hasSignal = hasSignal(randperm(length(hasSignal)));
         obj.Data.hasSignal = [obj.Data.hasSignal hasSignal];
      end
   end

end
