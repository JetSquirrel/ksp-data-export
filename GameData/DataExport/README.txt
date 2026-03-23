  _  ______  ____    ____        _          _____                       _   
 | |/ / ___||  _ \  |  _ \  __ _| |_ __ _  | ____|_  ___ __   ___  _ __| |_ 
 | ' /\___ \| |_) | | | | |/ _` | __/ _` | |  _| \ \/ / '_ \ / _ \| '__| __|
 | . \ ___) |  __/  | |_| | (_| | || (_| | | |___ >  <| |_) | (_) | |  | |_ 
 |_|\_\____/|_|     |____/ \__,_|\__\__,_| |_____/_/\_\ .__/ \___/|_|   \__|
                                                      |_|                   
Version: 1.0
__________________________________________________________________________________________


INFO

This mod was created by kna27.
GitHub repo: https://github.com/kna27/ksp-data-export


ABOUT

This mod allows you to export your flight data to a CSV file. You are then able to later create charts, graphs, etc. through software such as Excel or Google Sheets. You are able to choose which values you would like to log to the CSV, as well as how often data is logged.

Additionally, this mod can export data in Prometheus format for real-time monitoring. Enable it in logged.vals by setting prometheusEnabled=True. The metrics will be available at http://localhost:9101/metrics.

If you want to log every flight by default, open the logged.vals file and change defaultLogState to True.


PROMETHEUS EXPORTER

The Prometheus exporter feature allows you to scrape flight data in real-time using Prometheus or any compatible monitoring system.

Configuration (in logged.vals):
- prometheusEnabled=True/False : Enable or disable the Prometheus HTTP server
- prometheusPort=9101 : The port number for the HTTP server (default: 9101)

Once enabled, metrics are available at:
- http://localhost:9101/metrics : Prometheus metrics endpoint
- http://localhost:9101/ : Basic info page

The exporter provides metrics for all enabled loggable values in Prometheus format with labels for vessel name and category.


BUGS

If you encounter any bugs or have any suggestions, report them on the GitHub repo at https://github.com/kna27/ksp-data-export/issues.


INSTALLATION

Make sure you have downloaded the GameData/DataExport folder.
Move the DataExport folder to YourKSPInstallDirectory/Kerbal Space Program/GameData.
If done correctly, your directory should look like YourKSPInstallDirectory/Kerbal Space Program/GameData/DataExport.
