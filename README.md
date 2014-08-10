ssp4-control-software
=====================

This repository contains an open-source control software suite for Optec's
SSP-4 photometer (NIR J/H-band) distributed under the GPLv3 license.

The Optec SSP-4 is a JH-band single channel photometer with an RS-232 interface. 
After using the stock software for a few months, it became clear that additional
capabilities were needed. This control software permits integration times from 
0.01 to 65.53 seconds and 1-9999 exposures. It also features some quick-change 
buttons for the calibrator, target, sky and dark settings, a current temperature
readout, a scan status bar, a start/stop scan button, and an unlimited-length 
comment field. 

This software is written in C# using Microsoft Visual Studio 2005. It is
compatable with Linux and Mac when executed using Mono. Output data is stored
in a formatted text file. 

Compiled executables can be found in the `SSP4Installer/Release` directory.
The main source tree is in the SSP-4 directory.

This project is no longer actively maintained. If you wish to take over
maintenance of this package, please contact me (Brian Kloppenborg) using my
contact information on GitHub.
