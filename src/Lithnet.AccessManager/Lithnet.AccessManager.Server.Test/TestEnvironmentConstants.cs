using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Test
{
    public static class TestEnvironmentConstants
    {
        public const string Dev = "DEV";
        public const string SubDev = "SUBDEV";
        public const string ExtDev = "EXTDEV";

        public const string DevDc = "dc1.dev.lithnet.local";
        public const string SubDevDc = "dc2.sub.dev.lithnet.local";
        public const string ExtDevDc = "dc3.extdev.lithnet.local";

        public const string DevDefaultSite = "Dev-First-Site";
        public const string SubDevDefaultSite = "Dev-First-Site";
        public const string ExtDevDefaultSite = "ExtDev-First-Site";

        public const string DevLocal = "dev.lithnet.local";
        public const string SubDevLocal = "sub.dev.lithnet.local";
        public const string ExtDevLocal = "extdev.lithnet.local";

        public const string DevDN = "DC=DEV,DC=LITHNET,DC=LOCAL";
        public const string AmsTesting_DevDN = "OU=AMS Testing," + DevDN;
        public const string Users_AmsTesting_DevDN = "OU=Users," + AmsTesting_DevDN;
        public const string Computers_AmsTesting_DevDN = "OU=Computers," + AmsTesting_DevDN;
        public const string Groups_AmsTesting_DevDN = "OU=Groups," + AmsTesting_DevDN; 
        public const string JitGroups_AmsTesting_DevDN = "OU=JIT Groups," + Groups_AmsTesting_DevDN;
        public const string DynamicJitGroups_AmsTesting_DevDN = "OU=Dynamic JIT Groups," + Groups_AmsTesting_DevDN;

        public const string SubDevDN = "DC=SUB,DC=DEV,DC=LITHNET,DC=LOCAL";
        public const string AmsTesting_SubDevDN = "OU=AMS Testing," + SubDevDN;
        public const string Users_AmsTesting_SubDevDN = "OU=Users," + AmsTesting_SubDevDN;
        public const string Computers_AmsTesting_SubDevDN = "OU=Computers," + AmsTesting_SubDevDN;
        public const string Groups_AmsTesting_SubDevDN = "OU=Groups," + AmsTesting_SubDevDN;
        public const string JitGroups_AmsTesting_SubDevDN = "OU=JIT Groups," + Groups_AmsTesting_SubDevDN;
        public const string DynamicJitGroups_AmsTesting_SubDevDN = "OU=Dynamic JIT Groups," + Groups_AmsTesting_SubDevDN;

        public const string ExtDevDN = "DC=EXTDEV,DC=LITHNET,DC=LOCAL";
        public const string AmsTesting_ExtDevDN = "OU=AMS Testing," + ExtDevDN;
        public const string Users_AmsTesting_ExtDevDN = "OU=Users," + AmsTesting_ExtDevDN;
        public const string Computers_AmsTesting_ExtDevDN = "OU=Computers," + AmsTesting_ExtDevDN;
        public const string Groups_AmsTesting_ExtDevDN = "OU=Groups," + AmsTesting_ExtDevDN;
        public const string JitGroups_AmsTesting_ExtDevDN = "OU=JIT Groups," + Groups_AmsTesting_ExtDevDN;
        public const string DynamicJitGroups_AmsTesting_ExtDevDN = "OU=Dynamic JIT Groups," + Groups_AmsTesting_ExtDevDN;

        public const string LocalAccountTestUser = "testlocaluser1";

        public const string User1 = "user1";
        public const string User2 = "user2";
        public const string User3 = "user3";

        public const string G_GG_1 = "G-GG-1";
        public const string G_GG_2 = "G-GG-2";
        public const string G_GG_3 = "G-GG-3";

        public const string G_DL_1 = "G-DL-1";
        public const string G_DL_2 = "G-DL-2";
        public const string G_DL_3 = "G-DL-3";

        public const string G_UG_1 = "G-UG-1";
        public const string G_UG_2 = "G-UG-2";
        public const string G_UG_3 = "G-UG-3";

        public const string PC1 = "PC1";
        public const string PC2 = "PC2";
        public const string PC3 = "PC3";

        public const string PC1_D = PC1 + "$";
        public const string PC2_D = PC2 + "$";
        public const string PC3_D = PC3 + "$";

        public const string DEV_G_GG_1 = Dev + "\\" + G_GG_1;
        public const string DEV_G_GG_2 = Dev + "\\" + G_GG_2;
        public const string DEV_G_GG_3 = Dev + "\\" + G_GG_3;

        public const string DEV_G_UG_1 = Dev + "\\" + G_UG_1;
        public const string DEV_G_UG_2 = Dev + "\\" + G_UG_2;
        public const string DEV_G_UG_3 = Dev + "\\" + G_UG_3;

        public const string DEV_G_DL_1 = Dev + "\\" + G_DL_1;
        public const string DEV_G_DL_2 = Dev + "\\" + G_DL_2;
        public const string DEV_G_DL_3 = Dev + "\\" + G_DL_3;

        public const string DEV_User1 = Dev + "\\" + User1;
        public const string DEV_User2 = Dev + "\\" + User2;
        public const string DEV_User3 = Dev + "\\" + User3;

        public const string DEV_PC1 = Dev + "\\" + PC1;
        public const string DEV_PC2 = Dev + "\\" + PC2;
        public const string DEV_PC3 = Dev + "\\" + PC3;

        public const string DEV_PC1_D = Dev + "\\" + PC1_D;
        public const string DEV_PC2_D = Dev + "\\" + PC2_D;
        public const string DEV_PC3_D = Dev + "\\" + PC3_D;

        public const string DEV_JIT_PC1 = Dev + "\\JIT-" + PC1;
        public const string DEV_JIT_PC2 = Dev + "\\JIT-" + PC2;
        public const string DEV_JIT_PC3 = Dev + "\\JIT-" + PC3;

        public const string SUBDEV_User1 = SubDev + "\\" + User1;
        public const string SUBDEV_User2 = SubDev + "\\" + User2;
        public const string SUBDEV_User3 = SubDev + "\\" + User3;

        public const string SUBDEV_G_GG_1 = SubDev + "\\" + G_GG_1;
        public const string SUBDEV_G_GG_2 = SubDev + "\\" + G_GG_2;
        public const string SUBDEV_G_GG_3 = SubDev + "\\" + G_GG_3;

        public const string SUBDEV_G_UG_1 = SubDev + "\\" + G_UG_1;
        public const string SUBDEV_G_UG_2 = SubDev + "\\" + G_UG_2;
        public const string SUBDEV_G_UG_3 = SubDev + "\\" + G_UG_3;

        public const string SUBDEV_G_DL_1 = SubDev + "\\" + G_DL_1;
        public const string SUBDEV_G_DL_2 = SubDev + "\\" + G_DL_2;
        public const string SUBDEV_G_DL_3 = SubDev + "\\" + G_DL_3;

        public const string SUBDEV_PC1 = SubDev + "\\" + PC1;
        public const string SUBDEV_PC2 = SubDev + "\\" + PC2;
        public const string SUBDEV_PC3 = SubDev + "\\" + PC3;

        public const string SUBDEV_PC1_D = SubDev + "\\" + PC1_D;
        public const string SUBDEV_PC2_D = SubDev + "\\" + PC2_D;
        public const string SUBDEV_PC3_D = SubDev + "\\" + PC3_D;

        public const string SUBDEV_JIT_PC1 = SubDev + "\\JIT-" + PC1;
        public const string SUBDEV_JIT_PC2 = SubDev + "\\JIT-" + PC2;
        public const string SUBDEV_JIT_PC3 = SubDev + "\\JIT-" + PC3;

        public const string EXTDEV_User1 = ExtDev + "\\" + User1;
        public const string EXTDEV_User2 = ExtDev + "\\" + User2;
        public const string EXTDEV_User3 = ExtDev + "\\" + User3;

        public const string EXTDEV_G_GG_1 = ExtDev + "\\" + G_GG_1;
        public const string EXTDEV_G_GG_2 = ExtDev + "\\" + G_GG_2;
        public const string EXTDEV_G_GG_3 = ExtDev + "\\" + G_GG_3;

        public const string EXTDEV_G_UG_1 = ExtDev + "\\" + G_UG_1;
        public const string EXTDEV_G_UG_2 = ExtDev + "\\" + G_UG_2;
        public const string EXTDEV_G_UG_3 = ExtDev + "\\" + G_UG_3;

        public const string EXTDEV_G_DL_1 = ExtDev + "\\" + G_DL_1;
        public const string EXTDEV_G_DL_2 = ExtDev + "\\" + G_DL_2;
        public const string EXTDEV_G_DL_3 = ExtDev + "\\" + G_DL_3;

        public const string EXTDEV_PC1 = ExtDev + "\\" + PC1;
        public const string EXTDEV_PC2 = ExtDev + "\\" + PC2;
        public const string EXTDEV_PC3 = ExtDev + "\\" + PC3;

        public const string EXTDEV_PC1_D = ExtDev + "\\" + PC1_D;
        public const string EXTDEV_PC2_D = ExtDev + "\\" + PC2_D;
        public const string EXTDEV_PC3_D = ExtDev + "\\" + PC3_D;

        public const string EXTDEV_JIT_PC1 = ExtDev + "\\JIT-" + PC1;
        public const string EXTDEV_JIT_PC2 = ExtDev + "\\JIT-" + PC2;
        public const string EXTDEV_JIT_PC3 = ExtDev + "\\JIT-" + PC3;

    }
}
