# RevitConnectAINSCP

Revit Addin to connect with SAP AIN platform and update parameters

INSTALLATION (USE):

Clone git repo. I recommend clonning into Desktop for testing. 

Change the .addin 3 line (commented lines do not count) and point to the .dll file cloned (Usually you have to change only the username from jaime.hernandez to yours...)

Open Revit and test the plugin.

DEBUG (DEVELOPERS):

To run this project first you have to install Revit (My current version is 2019) and Microsoft Visual Studio (I have the community free edition)

After clonning the repository, you have to open it with visual studio. You have to edit the .addin file specifing where is the .dll compiled file of the addin.

Then, you have to put the .addin file inside the folder containing the different addins you have previously installed.

To save time, the .dll file path of the addin can be inside the bin\Debug\ folder, so you don´t have to copy and paste that file to any other directory after compiling the project each time. This does not apply for productive environments.

For debugging, you have to specify where is your Revit.exe file on your system, that way debugging is enabled on vs (Mine is C:\Program Files\Autodesk\Revit 2019\Revit.exe)

Then you press Start and set some breaKpoints.

