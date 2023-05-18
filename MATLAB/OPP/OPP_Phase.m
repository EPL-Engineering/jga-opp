classdef OPP_Phase
   % OPP_PHASE -- encapsulates properties and methods for executing an
   % OPP phase.

   properties (SetAccess = private, GetAccess = public)
      Number
      StimValue
      Data
      Started = false;
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
            obj.passIf = obj.settings.passBounceBackIf;
         end

         obj.Started = false;
         obj.trialNum = 1;
         obj.lastN_nosignal = -1;
         obj.lastN_signal = -1;

         obj.Data = obj.CreateDataStructure(number, value, direction);
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

         lastN = find(~obj.Data.hasSignal(1:obj.trialNum), obj.passIf.outOfNumNonSignal, 'last');
         if lastN >= obj.passIf.outOfNumNonSignal
            obj.lastN_nosignal = sum(obj.Data.correct(lastN));
         end
         nosigCritMet = obj.lastN_nosignal >= obj.passIf.numNonSignalCorrect;

         lastN = find(obj.Data.hasSignal(1:obj.trialNum), obj.passIf.outOfNumSignal, 'last');
         if lastN >= obj.passIf.outOfNumSignal
            obj.lastN_signal = sum(obj.Data.correct(lastN));
         end
         sigCritMet = obj.lastN_signal >= obj.passIf.numSignalCorrect;

         result = 'continue';
         message = '';

         if nosigCritMet && sigCritMet
            result = 'passed';
            message = sprintf('Satisfied hit (%d / %d) and false alarm (%d /%d) criteria', ...
               obj.passIf.numSignalCorrect, obj.passIf.outOfNumSignal, ...
               obj.passIf.numNonSignalCorrect, obj.passIf.outOfNumNonSignal ...
               );
         elseif obj.trialNum > obj.passIf.maxNumTrials && obj.settings.bounceBack.ifMaxNumTrials
            result = 'failed';
            message = sprintf('Reached maximum number of trials allowed (%d)', obj.passIf.maxNumTrials);
         else
            if isequal(obj.settings.bounceBack.consecutiveMissType, 'signals')
               lastN = find(obj.Data.hasSignal(1:obj.trialNum), obj.settings.bounceBack.ifNumConsecutiveMiss, 'last');
               if obj.Data.correct(lastN) >= obj.settings.bounceBack.ifNumConsecutiveMiss
                  result = 'failed';
                  message = sprintf('Missed %d signals in a row', obj.settings.bounceBack.ifNumConsecutiveMiss);
               end
            elseif isequal(obj.settings.bounceBack.consecutiveMissType, 'trials')
               if length(obj.Data.correct) >= obj.settings.bounceBack.ifNumConsecutiveMiss
                  c = flip(obj.Data.correct);
                  if all(c(1:obj.settings.bounceBack.ifNumConsecutiveMiss))
                     result = 'failed';
                     message = sprintf('Missed %d trials in a row', obj.settings.bounceBack.ifNumConsecutiveMiss);
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
            D{1,2} = sprintf('%d / %d', obj.lastN_nosignal, obj.passIf.outOfNumNonSignal);
         end
         D{3,1} = sprintf('%d', length(obj.Data.correct));
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
