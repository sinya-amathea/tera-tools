# tera-tools
Various tools to work with TERA related data

## ItemDataBrowser

This tool loads all ItemData-xxxxx.xml files into memory and provides functions to filter and export item data.


### Commands
|Name|Example|Description|
|---|---|---|
|**dd**|dd(name\[,column,...\|$columnSet$])|Print the dataset to the console. \[Optional\] specify a list of columns or saved column set.|
|**ddc**||Displays the datacenter path|
|**ex**|ex(name,format,target\[,columns,...\|$columnSet$\])|Export data in various formats. Format: colList, csv, json. Target: file, console.|
|**fd**|fd(name\[,filter\])|Create a filtered dataset. Use the name of xml attribute and its value to filter, combine filters with '&' and '\|'. Use only the name parameter to load a saved filter|
|**help**||Displays a list of commands|
|**map**|map\[(name)\]|Get a list of filter property to xml attribute mapping (filter properties usually are just PascalCase'd xml attribute names). \[Optional\] specify a part of a filter name to filter the list.|
|**lc**||List all saved column sets.|
|**lf**||Lists all saved filters.|
|**sc**|sc(name,columns,...)|Saves a list of validated columns to the settings file.|
|**sdc**|sdc(path)|Saves the datacenter path to settings file|
|**sf**|sf(name,filter)|Saves a validated filter definition to the settings file.|
|**sn**|sn(string\[,includeToolTip])|Search an item by it's name, enclose with \" to do an exact search. \[Optional\] use true/fals to include/exclude searching in tooltips|

### Filtering

Define a condition with this syntax `Id==123456`, you can use `&` (and) and `|` (or) to combine conditions.
Brackets are not supported... yet.
Following operators can be used (not all are fully tested yet)

|Operator|Description|
|---|---|
|**==**|Equal|
|**!=**|Not Equal|
|**?=**|Contains or Like|
|**SW**|Starts with *not tested*|
|**EW**|Ends with *not tested*|
|**BT**|Between (BTvalue1,value2) *not tested*|
|**>>**|Greater|
|**<<**|Smaller|
|**>=**|Greater or Equal|
|**<=**|Smaller or Equal|

#### Examples

All elin costumes `CombatItemType==EQUIP_STYLE_BODY & RequiredGender==female & RequiredRace==popori`  
All castanic male costumes `CombatItemType==EQUIP_STYLE_BODY & RequiredGender==male & RequiredRace==castanic`

### Exporting

The export function primarely used is `colList`, it outputs a string of one (the first) given column separated by `,`

It can export as `colList`, `json` and `csv` to either a `file` or directly to `console`.  
**Note:** `colList` only uses the first specified column in case multiple column were specified.  
(`json` and `csv` maybe buggy depending on selected data and columns.)

### Future improvements
- Clean up the code, its a huge mess... :P
- Load StrSheet_Item-xxxxx.xml files and add functions to search by item name and description
- Test and improve filter function
