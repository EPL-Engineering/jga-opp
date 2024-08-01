function success = motu_set_laterality_monitor8(baseURL, ear)

baseURL = [baseURL '/datastore/'];
% if nargin < 2, baseURL = "http://169.254.221.196/datastore/"; end

success = true;

try
   % Connect mixer outputs to analog outputs
   src = -1;
   if strcmpi(ear, 'right') || strcmpi(ear, 'both')
      src = 1;
   end
   webwrite(baseURL + "ext/obank/2/ch/0/src", sprintf('json={"value":"16:%d"}', src));

   src = -1;
   if strcmpi(ear, 'left') || strcmpi(ear, 'both')
      src = 1;
   end
   webwrite(baseURL + "ext/obank/2/ch/1/src", sprintf('json={"value":"16:%d"}', src));
catch ex
   success = false;
   warning(ex.message);
end