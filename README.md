# tera-tools
Various tools to work with TERA related data

## ItemDataBrowser

This tool loads all ItemData-xxxxx.xml files into memory and provides functions to filter and export item data.


### Commands
|Name|Example|Description|
|---|---|---|
|**ddc**||Displays the datacenter path|
|**sdc**|sdc(path)|Saves the datacenter path to settings file|
|**fd**|fd(name\[,filter\])|Create a filtered dataset. Use the name of xml attribute and its value to filter, combine filters with '&' and '\|'. Use only the name parameter to load a saved filter|
|**dd**|dd(name\[,column,...\|$columnSet$])|Print the dataset to the console. \[Optional\] specify a list of columns or saved column set.|
|**sf**|sf(name,filter)|Saves a validated filter definition to the settings file.|
|**lf**||Lists all saved filters.|
|**ex**|ex(name,format,target\[,columns,...\|$columnSet$\])|Export data in various formats. Format: colList, csv, json. Target: file, console.|
|**sc**|sc(name,columns,...)|Saves a list of validated columns to the settings file.|
|**lc**||List all saved column sets.|
|**map**|map\[(name)\]|Get a list of filter property to xml attribute mapping (filter properties usually are just PascalCase'd xml attribute names). \[Optional\] specify a part of a filter name to filter the list.|
|**help**||Displays a list of commands|

### Filtering

Define a condition with this syntax `Id==123456`, you can use `&` (and) and `|` (or) to combine conditions.
Brackets are not supported... yet.
Following operators can be used (not all are fully tested yet)

|Operator|Description|
|---|---|
|**==**|Equal|
|**!=**|Not Equal|
|**?=**|Contains or Like|
|**SW**|Starts with|
|**EW**|Ends with|
|**BT**|Between (BTvalue1,value2) *not tested*|
|**>>**|Greater|
|**<<**|Smaller|
|**>=**|Greater or Equal|
|**<=**|Smaller or Equal|

#### Examples

All elin costumes `CombatItemType==EQUIP_STYLE_BODY & RequiredGender==female & RequiredRace==popori`  
All castanic male costumes `CombatItemType==EQUIP_STYLE_BODY & RequiredGender==male & RequiredRace==castanic`

### Future improvements
- Clean up the code, its a huge mess... :P
- Load StrSheet_Item-xxxxx.xml files and add functions to search by item name and description
- Test and improve filter function
