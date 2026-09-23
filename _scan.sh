#!/bin/bash
SRC=D:/idocNet/2019.6.InBrandCloud/Dev/V21
echo "===== Event.cs methods ====="
grep -nE 'public [A-Za-z_<>]+ [A-Za-z_]+\(' "$SRC/idn.Skycic.InBrand.Biz/eTEMTruyXuat/Event.cs" | head -60
echo "===== eTemNN.cs methods ====="
grep -nE 'public [A-Za-z_<>]+ [A-Za-z_]+\(' "$SRC/idn.Skycic.InBrand.Biz/eTEMTruyXuat/eTemNN.cs" | head -80
echo "===== MstTemplate.cs methods ====="
grep -nE 'public [A-Za-z_<>]+ [A-Za-z_]+\(' "$SRC/idn.Skycic.InBrand.Biz/eTEMTruyXuat/MstTemplate.cs" | head -60
