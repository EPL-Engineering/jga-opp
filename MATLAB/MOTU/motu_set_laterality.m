% function motu_set_laterality(baseURL)

baseURL = "http://169.254.128.32/datastore/";

% Connect mixer outputs to analog outputs
webwrite(baseURL + "ext/obank/0/ch/0/src", 'json={"value":"16:0"}');
% webwrite(baseURL + "ext/obank/0/ch/1/src", 'json={"value":"16:0"}');
% webwrite(baseURL + "ext/obank/0/ch/2/src", 'json={"value":"16:1"}');
% webwrite(baseURL + "ext/obank/0/ch/3/src", 'json={"value":"16:1"}');
% webwrite(baseURL + "ext/obank/0/ch/4/src", 'json={"value":"16:2"}');
