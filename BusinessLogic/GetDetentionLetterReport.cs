using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DA = CONTECH.Service.DataAccess;

namespace CONTECH.Service.BusinessLogic
{
    public enum LettersType
    {
        ArmortecSubmittal = 1,
        CMPDetentionLetter = 2,
        CMPLargeDiameterLetter = 3,
        DuroMaxxCisternRWHLetter = 4,
        DuroMaxxContainmentTankNotificationLetter = 5,
        DuroMaxxDetentionLetter = 6,
        DuroMaxxLgDiameterLetter = 7,
        DuroMaxxSewerLetter=8
    }
    public class GetDetentionLetterReport
    {
        public List<LettersType> GetReportListForDownload(string salesorderid)
        {
            List<LettersType> lstlettersTypes = new List<LettersType>();

            try
            {
                DA.OrderProductRepositry productdetail = new DA.OrderProductRepositry();
                DataTable dtSalesOrderDetail = productdetail.GetOrderProductDetail(salesorderid);


                if (dtSalesOrderDetail != null && dtSalesOrderDetail.Rows.Count > 0)
                {
                    foreach (DataRow drSalesOrderDetail in dtSalesOrderDetail.Rows)
                    {
                        if ((Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "CMP Detention") ||
                            (Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "CMP Detention - Voidsaver") ||
                            (Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "CMP Detention - xFiltration")) //Added 602.2
                        {
                            lstlettersTypes.Add(LettersType.CMPDetentionLetter);
                            continue;
                        }
                        else if (Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "DuroMaxx Containment Tank")
                        {
                            lstlettersTypes.Add(LettersType.DuroMaxxContainmentTankNotificationLetter);
                            continue;
                        }
                        else if ((Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "DuroMaxx Detention") ||
                                (Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "DuroMaxx Detention - VoidSaver"))
                        {
                            lstlettersTypes.Add(LettersType.DuroMaxxDetentionLetter);
                            continue;
                        }
                        else if (Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "UrbanGreen SRPE Cistern")
                        {
                            lstlettersTypes.Add(LettersType.DuroMaxxCisternRWHLetter);
                            continue;
                        } else if (Convert.ToString(drSalesOrderDetail["productfamily"]).Trim() == "DuroMaxx Sewer") 
                        {
                            lstlettersTypes.Add(LettersType.DuroMaxxSewerLetter);
                            continue;
                        }

                        string strPartNo = Convert.ToString(drSalesOrderDetail["PartNo"]).Trim().ToLower();
                        string strShape = Convert.ToString(drSalesOrderDetail["Shape"]).Trim().ToLower();
                        string strToMailAddress = Convert.ToString(drSalesOrderDetail["Email"]).Trim();

                        string strOrderNumber = Convert.ToString(drSalesOrderDetail["Ordernumber"]).Trim();

                        if (!string.IsNullOrEmpty(strPartNo))
                        {
                            string strPartGroup = strPartNo.Substring(0, 2);

                            if (strPartNo.Length > 3)
                            {
                                string strPartNo1 = strPartNo.Substring(0, 2);
                                string strPartNo2 = strPartNo.Substring(3, 1);
                                string strPartNo3 = strPartNo.Substring(0, 3);
                                bool isLarger = false;

                                string strCorr;
                                string strDiam;
                                #region FOR LARGE DIAMETER PIPE ON HEL-COR AND RIVETED PIPE

                                //Change Condition for Send Reminder mail for Hel-Cor Pipe Arch and Hel-Cor Elongate
                                if ((strPartNo1.Equals("hp") ||
                                    strPartNo1.Equals("hc") ||
                                    strPartNo1.Equals("he") ||
                                    strPartNo1.Equals("rp") ||
                                    strPartNo1.Equals("re") ||
                                    strPartNo1.Equals("rh")) && strPartNo.Length >= 10)
                                {
                                    strCorr = strPartNo.Substring(2, 1);
                                    string strGrade = strPartNo.Substring(3, 2);
                                    string strGage;
                                    if (strPartNo1.Equals("hp") || strPartNo1.Equals("hc") || strPartNo1.Equals("he"))
                                    {
                                        strGage = strPartNo.Substring(6, 2);
                                        strDiam = strPartNo.Substring(8, 3);
                                    }
                                    else
                                    {
                                        strGage = strPartNo.Substring(5, 2);
                                        strDiam = strPartNo.Substring(7, 3);
                                    }
                                    int.TryParse(strDiam, out int intDiam);

                                    switch (strCorr)
                                    {
                                        case "2":
                                            isLarger = CheckAluNonAluCorrugation(strGrade, intDiam, strGage);
                                            break;

                                        case "3":
                                            isLarger = CheckAluCorrugation(strGrade, intDiam);
                                            break;

                                        case "5":
                                            isLarger = CheckAluCorrugation(strGrade, intDiam);
                                            break;

                                        case "s":
                                            isLarger = CheckNonAluUltraFlo(strGrade, strGage, strPartGroup, intDiam);
                                            break;
                                    }

                                    if (isLarger)
                                    {
                                        lstlettersTypes.Add(LettersType.CMPLargeDiameterLetter);
                                        continue;
                                    }
                                }

                                #endregion FOR LARGE DIAMETER PIPE ON HEL-COR AND RIVETED PIPE

                                #region FOR Double Wall Hel-Cor Pipe or Double Wall Hel-Cor Pipe-Arch:

                                if ((strPartNo1.Equals("dw") || strPartNo1.Equals("da")) && strPartNo.Length >= 14)
                                {
                                    strCorr = strPartNo.Substring(2, 1);
                                    strDiam = strPartNo.Substring(11, 3);
                                    int.TryParse(strDiam, out int intDiam);

                                    switch (strCorr)
                                    {
                                        case "2":
                                            if (intDiam > 77)
                                            {
                                                isLarger = true;
                                            }
                                            break;

                                        case "3":
                                            if (intDiam > 101)
                                            {
                                                isLarger = true;
                                            }
                                            break;
                                    }

                                    if (isLarger)
                                    {
                                        lstlettersTypes.Add(LettersType.CMPLargeDiameterLetter);
                                        continue;
                                    }
                                }

                                #endregion FOR Double Wall Hel-Cor Pipe or Double Wall Hel-Cor Pipe-Arch:

                                #region FOR DUROMAXX Pipes

                                if (strPartNo3.Equals("xpg") && strPartNo.Length >= 8)
                                {
                                    strDiam = strPartNo.Substring(5, 3);
                                    int.TryParse(strDiam, out int intDiam);
                                    if (intDiam > 72)
                                    {
                                        isLarger = true;
                                    }
                                    if (isLarger)
                                    {
                                        lstlettersTypes.Add(LettersType.DuroMaxxLgDiameterLetter);
                                        continue;
                                    }
                                }


                                #endregion FOR DUROMAXX Pipes

                                #region FOR URBAN GREEN SRPE AND URBAN GREEN UGM

                                if ((strPartNo3.Equals("ugu") || strPartNo3.Equals("ugs")) && strPartNo.Length >= 7)
                                {
                                    switch (strPartNo3)
                                    {
                                        case "ugu":
                                            strDiam = strPartNo.Substring(5, 2);
                                            if (strPartNo.Length > 7)
                                            {
                                                strDiam = strPartNo.Substring(5, 3);
                                            }
                                            int.TryParse(strDiam, out int intDiam);
                                            if (intDiam >= 72)
                                            {
                                                isLarger = true;
                                            }
                                            break;

                                        case "ugs":
                                            isLarger = true;
                                            break;

                                        default:
                                            break;
                                    }
                                    if (isLarger)
                                    {
                                        lstlettersTypes.Add(LettersType.DuroMaxxCisternRWHLetter);
                                        continue;
                                    }
                                }

                                #endregion FOR URBAN GREEN SRPE AND URBAN GREEN UGM
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return lstlettersTypes.Distinct().ToList();
        }

        /// <summary>
        /// Check for if corrugation is 3 or 5.
        /// </summary>
        /// <returns>boolean</returns>
        private bool CheckAluCorrugation(string strGrade, int intDiam)
        {
            if ((strGrade.Equals("al") && intDiam > 71) || (strGrade.Equals("al") == false && intDiam > 101))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if product is Alumnium or NonAluminium Corrugation.
        /// </summary>
        /// <returns>boolean</returns>
        private bool CheckAluNonAluCorrugation(string strGrade, int intDiam, string strGage)
        {
            //Aluminum /NoAlluninum 2-2/3x1/2 corrugation
            if ((strGrade.Equals("al") && intDiam > 59) ||
                //Non-Aluminum 2-2/3x1/2 corrugation
                (strGrade.Equals("al") == false && intDiam > 77))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check for Non Aluminium Utra flo product.
        /// </summary>
        /// <returns>boolean</returns>
        private bool CheckNonAluUltraFlo(string strGrade, string strGage, string strPartGroup, int intDiam)
        {
            if ((strGrade.Equals("al") == false && intDiam > 77) || (strGrade.Equals("al") && intDiam > 59))
            {
                return true;
            }
            return false;
        }
    }
}
