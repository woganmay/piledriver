# piledriver
One-way sync to move files from a Windows desktop, to a Drive account.

This is not even alpha yet. To call this software "alpha" would be an insult to all ambitious, incomplete software projects.

# Why

This solves a very specific need I have. I needed a way to automatically move (as in delete locally) low-importance files to the cloud on a regular basis, and have them grouped up by day. Google Drive happens to be my cloud poison of choice.

# Setup

1. Clone the repo
1. Get a client_id.json file from [this wizard](https://console.developers.google.com/start/api?id=drive)
1. Drop that client_id.json file into Visual Studio
1. The root backup path is configured as a static constant (C:\PileDriver)
1. Run


