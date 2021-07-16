using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using DisAsm6502.Model;
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

namespace DisAsm6502.ViewModel
{
    /// <summary>
    /// Class to display model
    /// </summary>
    public class ViewModel : Notifier
    {
        private bool _rebuilding;

        public bool ReBuilding
        {
            get => _rebuilding;
            set
            {
                _rebuilding = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Dictionary to hold well known address and symbols
        /// </summary>
        private readonly Dictionary<int, string> _builtInSymbols = new Dictionary<int, string>
        {
            {0x0286, "TEXT"},
            {0x0300, "IERROR"},
            {0x0302, "IMAIN"},
            {0x0308, "IGONE"},
            {0xA00C, "STMDSP"},
            {0xA052, "FUNDSPTABLE"},
            {0xA080, "OPTAB"},
            {0xA09E, "RESLST"},
            {0xA19E, "ERRTAB"},
            {0xA38A, "FNDFOR"},
            {0xA3B8, "BLTU"},
            {0xA3FB, "GETSTK"},
            {0xA408, "REASON"},
            {0xA435, "OMERR"},
            {0xA437, "ERROR"},
            {0xA474, "READY"},
            {0xA480, "MAIN"},
            {0xA49C, "MAIN1"},
            {0xA533, "LINKPRG"},
            {0xA560, "INLIN"},
            {0xA579, "CRUNCH"},
            {0xA613, "FNDLIN"},
            {0xA642, "SCRTCH"},
            {0xA65E, "CLEAR"},
            {0xA68E, "RUNC"},
            {0xA69C, "LIST"},
            {0xA717, "QPLOP"},
            {0xA742, "FOR"},
            {0xA7AE, "NEWSTT"},
            {0xA7E4, "GONE"},
            {0xA81D, "RESTOR_"},
            {0xA82F, "STOP"},
            {0xA831, "END"},
            {0xA857, "CONT"},
            {0xA871, "RUN"},
            {0xA883, "GOSUB"},
            {0xA8A0, "GOTO"},
            {0xA8D2, "RETURN"},
            {0xA8F8, "DATA"},
            {0xA906, "DATAN"},
            {0xA928, "IF"},
            {0xA93B, "REM"},
            {0xA94B, "ONGOTO"},
            {0xA96B, "LINGET"},
            {0xA9A5, "LET"},
            {0xAA80, "PRINTN"},
            {0xAA86, "CMD"},
            {0xAAA0, "PRINT"},
            {0xAB1E, "STROUT"},
            {0xABA5, "DOAGAIN"},
            {0xAB7B, "GET"},
            // {0xABA5, "INPUTN"},
            {0xABBF, "INPUT"},
            {0xAC06, "READ"},
            {0xACFC, "EXIFNT"},
            {0xAD1D, "NEXT"},
            {0xAD8A, "FRMNUM"},
            {0xAD9E, "FRMEVL"},
            {0xAE83, "EVAL"},
            {0xAEA8, "PIVAL"},
            {0xAEF1, "PARCHK"},
            {0xAEF7, "CHKCLS"},
            {0xAEFA, "CHKOPN"},
            {0xAEFF, "CHKCOM"},
            {0xAF08, "SNERR"},
            {0xAF2B, "ISVAR"},
            {0xAFA7, "ISFUN"},
            {0xAFE6, "OROP"},
            {0xAFE9, "ANDOP"},
            {0xB016, "DORE1"},
            {0xB081, "DIM"},
            {0xB08B, "PTRGET"},
            {0xB11D, "NOTFNS"},
            {0xB185, "FINPTR"},
            {0xB194, "ARYGET"},
            {0xB1A5, "N32768"},
            {0xB1B2, "INTIDX"},
            {0xB1BF, "AYINT"},
            {0xB1D1, "ISARY"},
            {0xB245, "BSERR"},
            {0xB248, "FCERR"},
            {0xB34C, "UMULT"},
            {0xB37D, "FRE"},
            {0xB391, "GIVAYF"},
            {0xB39E, "POS"},
            {0xB3A6, "ERRDIR"},
            {0xB3B3, "DEF"},
            {0xB3E1, "GETFNM"},
            {0xB3F4, "FBDOER"},
            {0xB465, "STRD"},
            {0xB487, "STRLIT"},
            {0xB4F4, "GETSPA"},
            {0xB536, "GARBAG"},
            {0xB63D, "CAT"},
            {0xB67A, "MOVINS"},
            {0xB6A3, "FRESTR"},
            {0xB6DB, "FRETMS"},
            {0xB6EC, "CHRD"},
            {0xB700, "LEFTD"},
            {0xB72C, "RIGHTD"},
            {0xB737, "MIDD"},
            {0xB761, "PREAM"},
            {0xB77C, "LEN"},
            {0xB78B, "ASC"},
            {0xB79B, "GETBYTC"},
            {0xB7AD, "VAL"},
            {0xB7EB, "GETNUM"},
            {0xB7F7, "GETADR"},
            {0xB80D, "PEEK"},
            {0xB824, "POKE"},
            {0xB82D, "FUWAIT"},
            {0xB849, "FADDH"},
            {0xB850, "FSUB"},
            {0xB853, "FSUBT"},
            {0xB867, "FADD"},
            {0xB86A, "FADDT"},
            {0xB8A7, "FADD4"},
            {0xB8FE, "NORMAL"},
            {0xB947, "NEGFAC"},
            {0xB97E, "OVERR"},
            {0xB983, "MULSHF"},
            {0xB9BC, "FONE"},
            {0xB9C1, "LOGCN2"},
            {0xB9EA, "LOG"},
            {0xBA28, "FMULT"},
            {0xBA33, "FMULT1"},
            {0xBA59, "MLTPLY"},
            {0xBA8C, "CONUPK"},
            {0xBAB7, "MULDIV"},
            {0xBAD4, "MLDVEX"},
            {0xBAE2, "MUL10"},
            {0xBA79, "TENC"},
            {0xBAFE, "DIV10"},
            {0xBB0F, "FDIV"},
            {0xBB12, "FDIVT"},
            {0xBBA2, "MOVFM"},
            {0xBBC7, "MOV2F"},
            {0xBBFC, "MOVFA"},
            {0xBC0C, "MOVAF"},
            {0xBC0F, "MOVEF"},
            {0xBC1B, "ROUND"},
            {0xBC2B, "SIGN"},
            {0xBC39, "SGN"},
            {0xBC58, "ABS"},
            // {0xBC5B, "FCOMP"},
            {0xBC9B, "QINT"},
            {0xBCCC, "INT"},
            {0xBCF3, "FIN"},
            {0xBD7E, "FINLOG"},
            {0xBDC0, "N0999"},
            {0xBDCD, "LINPRT"},
            {0xBDDD, "FOUT"},
            {0xBF11, "FHALF"},
            {0xBF1C, "FOUTBL"},
            {0xBF3A, "FDCEND"},
            {0xBF71, "SQR"},
            {0xBF7B, "FPWRT"},
            {0xBFB4, "NEGOP"},
            {0xBFBF, "EXPCON"},
            {0xBFED, "EXP"},
            {0xD020, "BORDER"},
            {0xD021, "SCREENC"},
            {0xDC0D, "CIAICR"},
            {0xDC0E, "CIACRA"},
            {0xDD0D, "CI2ICR"},
            {0xDD0E, "CI2CRA"},
            {0xE043, "POLY1"},
            {0xE059, "POLY2"},
            {0xE08D, "RMULC"},
            {0xE092, "RADDC"},
            {0xE097, "RND"},
            {0xE12A, "SYS"},
            {0xE156, "SAVE"},
            {0xE165, "VERIFY"},
            {0xE168, "LOAD_"},
            {0xE1BE, "OPEN"},
            {0xE1C7, "CLOSE"},
            {0xE264, "COS"},
            {0xE26B, "SIN"},
            {0xE2B4, "TAN"},
            {0xE2E0, "PI2"},
            {0xE2E5, "TWOPI"},
            {0xE2EA, "FR4"},
            {0xE2EF, "SINCON"},
            {0xE30E, "ATN"},
            {0xE33E, "ATNCON"},
            {0xE37B, "WARM"},
            {0xE394, "COLD"},
            {0xE3A2, "INITAT"},
            {0xE3BF, "INIT"},
            {0xE460, "WORDS"},
            {0xE500, "IOBASE"},
            {0xE505, "SCREEN"},
            {0xE50A, "PLOT"},
            {0xE5B4, "LP2"},
            {0xEA87, "SCNKEY"},
            {0xED09, "TALK"},
            {0xED0C, "LISTEN"},
            {0xEDB9, "SECOND"},
            {0xEDC7, "TKSA"},
            {0xEDDD, "CIOUT"},
            {0xEDFE, "UNTLK"},
            {0xEE13, "ACPTR"},
            {0xFFD5, "LOAD"},
            {0xFF8A, "RESTOR"},
            {0xFFBA, "SETLFS"},
            {0xFFBD, "SETNAM"},
            {0xF13E, "GETIN"},
            {0xF157, "CHRIN"},
            {0xF1CA, "_CHROUT"},
            {0xFFD2, "CHROUT"},
            {0x0277, "KEYD"},
            {0x00, "D6510"},
            {0x01, "R6510"},
            {0x02, "UNUSED1"},
            {0x03, "ADRAY1"},
            {0x05, "ADRAY2"},
            {0X07, "CHARAC"},
            {0X08, "ENDCHAR"},
            {0X09, "TRMPOS"},
            {0X0A, "VERCK"},
            {0X0B, "COUNT"},
            {0X0C, "DIMFLG"},
            {0X0D, "VALTYP"},
            {0X0E, "INTFLG"},
            {0X0F, "GARBFLG"},
            {0X10, "SUBFLG"},
            {0X11, "INPFLG"},
            {0X12, "TANSGN"},
            {0X13, "CHANNL"},
            {0X14, "LINNUM"},
            {0X16, "TEMPPT"},
            {0X17, "LASTPT"},
            {0X19, "TEMPST"},
            {0X22, "INDEX"},
            {0X26, "RESHO"},
            {0X2B, "TXTTAB"},
            {0X2D, "VARTAB"},
            {0X2F, "ARYTAB"},
            {0X31, "STREND"},
            {0X33, "FRETOP"},
            {0X35, "FRESPC"},
            {0X37, "MEMSIZ"},
            {0X39, "CURLIN"},
            {0X3B, "OLDLIN"},
            {0X3D, "OLDTXT"},
            {0X3F, "DATLIN"},
            {0X41, "DATPTR"},
            {0X43, "INPPTR"},
            {0X45, "VARNAM"},
            {0X47, "VARPNT"},
            {0X49, "FORPNT"},
            {0X4B, "OPPTR"},
            {0X4D, "OPMASK"},
            {0X4E, "DEFPNT"},
            {0X50, "DSCPNT"},
            {0X53, "FOUR6"},
            {0X54, "JMPER"},
            {0X57, "UNUSED2"},
            {0X61, "FAC1"},
            // {0X61, "FACEXP"},
            {0X62, "FACHO"},
            {0X66, "FACSSGN"},
            {0X67, "SGNFLG"},
            {0X68, "BITS"},
            {0X69, "FAC2"},
            // {0X69, "ARGEXP"},
            {0X6A, "ARGHO"},
            {0X6E, "ARGSGN"},
            {0X70, "FACOV"},
            {0X71, "FBUFPTR"},
            {0X73, "CHRGET"},
            {0X7A, "TXTPTR"},
            {0X8B, "RNDX"},
            {0X90, "STATUS"},
            {0X91, "STKEY"},
            {0X92, "SVXT"},
            {0X93, "VERCK2"},
            {0X94, "C3PO"},
            {0X95, "BSOUR"},
            {0X96, "SYNO"},
            {0X97, "XSAV"},
            {0X98, "LDTND"},
            {0X99, "DFLTN"},
            {0X9A, "DFLTO"},
            {0X9B, "PRTY"},
            {0X9C, "DPSW"},
            {0X9D, "MSGFLG"},
            {0X9E, "PTR1"},
            {0X9F, "PTR2"},
            {0XA0, "TIME"},
            {0XA3, "TMPDATA"},
            {0XA5, "CNTDN"},
            {0XA6, "BUFPNT"},
            {0XA7, "INBIT"},
            {0XA8, "BITCI"},
            {0XA9, "RINONE"},
            {0XAA, "RIDATA"},
            {0XAB, "RIPRTY"},
            {0XAC, "SAL"},
            {0XAE, "EAL"},
            {0XB0, "CMP0"},
            {0XB2, "TAPE1"},
            {0XB4, "BITTS"},
            {0XB5, "NXTBIT"},
            {0XB6, "RODATA"},
            {0XB7, "FNLEN"},
            {0XB8, "LA"},
            {0XB9, "SA"},
            {0XBA, "FA"},
            {0XBB, "FNADR"},
            {0XBD, "ROPRTY"},
            {0XBE, "FSBLK"},
            {0XBF, "MYCH"},
            {0XC0, "CAS1"},
            {0XC1, "STAL"},
            {0XC3, "MEMUSS"},
            {0XC5, "LSTX"},
            {0XC6, "NDX"},
            {0XC7, "RVS"},
            {0XC8, "INDX"},
            {0XC9, "LXSP"},
            {0XCB, "SFDX"},
            {0XCC, "BLNSW"},
            {0XCD, "BLNCT"},
            {0XCE, "GDBLN"},
            {0XCF, "BLNON"},
            {0XD0, "CRSW"},
            {0XD1, "PNT"},
            {0XD3, "PNTR"},
            {0XD4, "QTSW"},
            {0XD5, "LNMX"},
            {0XD6, "TBLX"},
            {0XD7, "UNUSED3"},
            {0XD8, "INSRT"},
            {0XD9, "LDTB1"},
            {0XF3, "USER"},
            {0XF5, "KEYTAB"},
            {0XF7, "RIBUF"},
            {0XF9, "ROBUF"},
            {0XFB, "FREKZP"},
            {0XFF, "BASZPT"},
        };

        private ObservableCollection<string> _symCollection = new ObservableCollection<string>();

        /// <summary>
        /// Holds symbols
        /// Backing data for top of file
        /// </summary>
        public ObservableCollection<string> SymCollection
        {
            get => _symCollection;
            set
            {
                _symCollection = value;
                OnPropertyChanged();
            }
        }

        private string _org;

        /// <summary>
        /// string representation of ORG directive
        /// </summary>
        public string Org
        {
            get => _org;
            set
            {
                _org = value;
                OnPropertyChanged();
            }
        }

        private int _loadAddress;

        /// <summary>
        /// Address the program loads at
        /// First 2 bytes of .prg file
        /// Causes Org to be recalculated
        /// </summary>
        public int LoadAddress
        {
            get => _loadAddress;
            set
            {
                _loadAddress = value;
                OnPropertyChanged();
            }
        }

        private byte[] _data;

        /// <summary>
        /// Bytes read from the .prg files
        /// Causes initial parse
        /// </summary>
        public byte[] Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Number of bytes usesed in each addressing mode
        /// </summary>
        private static readonly int[] ModeSizes =
        {
            1, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 2, 2, 1
        };

        /// <summary>
        /// Get number of bytes in a given addressing mode
        /// </summary>
        /// <param name="mode">addressing mode</param>
        /// <returns>number of bytes needed for mode</returns>
        private static int GetSize(AddressingModes mode)
        {
            return ModeSizes[(int) mode];
        }

        /// <summary>
        /// Array holding Opcodes, addressing mode and string name
        /// </summary>
        private static readonly Op[] Ops =
        {
            new Op("brk", AddressingModes.I),
            new Op("ora", AddressingModes.Zpix),
            new Op(),
            new Op(),
            new Op(),
            new Op("ora", AddressingModes.Zp),
            new Op("asl", AddressingModes.Zp),
            new Op(),
            new Op("php", AddressingModes.I),
            new Op("ora", AddressingModes.Im),
            new Op("asl", AddressingModes.I),
            new Op(),
            new Op(),
            new Op("ora", AddressingModes.A),
            new Op("asl", AddressingModes.A),
            new Op(),
            new Op("bpl", AddressingModes.R),
            new Op("ora", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op(),
            new Op("ora", AddressingModes.Zpx),
            new Op("asl", AddressingModes.Zpx),
            new Op(),
            new Op("clc", AddressingModes.I),
            new Op("ora", AddressingModes.Ay),
            new Op(),
            new Op(),
            new Op(),
            new Op("ora", AddressingModes.Ax),
            new Op("asl", AddressingModes.Ax),
            new Op(),
            new Op("jsr", AddressingModes.A),
            new Op("and", AddressingModes.Zpix),
            new Op(),
            new Op(),
            new Op("bit", AddressingModes.Zp),
            new Op("and", AddressingModes.Zp),
            new Op("rol", AddressingModes.Zp),
            new Op(),
            new Op("plp", AddressingModes.I),
            new Op("and", AddressingModes.Im),
            new Op("rol", AddressingModes.I),
            new Op(),
            new Op("bit", AddressingModes.A),
            new Op("and", AddressingModes.A),
            new Op("rol", AddressingModes.A),
            new Op(),
            new Op("bmi", AddressingModes.R),
            new Op("and", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op(),
            new Op("and", AddressingModes.Zpx),
            new Op("rol", AddressingModes.Zpx),
            new Op(),
            new Op("sec", AddressingModes.I),
            new Op("and", AddressingModes.Ay),
            new Op(),
            new Op(),
            new Op(),
            new Op("and", AddressingModes.Ax),
            new Op("rol", AddressingModes.Ax),
            new Op(),
            new Op("rti", AddressingModes.I),
            new Op("eor", AddressingModes.Zpix),
            new Op(),
            new Op(),
            new Op(),
            new Op("eor", AddressingModes.Zp),
            new Op("lsr", AddressingModes.Zp),
            new Op(),
            new Op("pha", AddressingModes.I),
            new Op("eor", AddressingModes.Im),
            new Op("lsr", AddressingModes.I),
            new Op(),
            new Op("jmp", AddressingModes.A),
            new Op("eor", AddressingModes.A),
            new Op("lsr", AddressingModes.A),
            new Op(),
            new Op("bvc", AddressingModes.R),
            new Op("eor", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op(),
            new Op("eor", AddressingModes.Zpx),
            new Op("lsr", AddressingModes.Zpx),
            new Op(),
            new Op("cli", AddressingModes.I),
            new Op("eor", AddressingModes.Ay),
            new Op(),
            new Op(),
            new Op(),
            new Op("eor", AddressingModes.Ax),
            new Op("lsr", AddressingModes.Ax),
            new Op(),
            new Op("rts", AddressingModes.I),
            new Op("adc", AddressingModes.Zpix),
            new Op(),
            new Op(),
            new Op(),
            new Op("adc", AddressingModes.Zp),
            new Op("ror", AddressingModes.Zp),
            new Op(),
            new Op("pla", AddressingModes.I),
            new Op("adc", AddressingModes.Im),
            new Op("ror", AddressingModes.I),
            new Op(),
            new Op("jmp", AddressingModes.Ind),
            new Op("adc", AddressingModes.A),
            new Op("ror", AddressingModes.A),
            new Op(),
            new Op("bvs", AddressingModes.R),
            new Op("adc", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op(),
            new Op("adc", AddressingModes.Zpx),
            new Op("ror", AddressingModes.Zpx),
            new Op(),
            new Op("sei", AddressingModes.I),
            new Op("adc", AddressingModes.Ay),
            new Op(),
            new Op(),
            new Op(),
            new Op("adc", AddressingModes.Ax),
            new Op("ror", AddressingModes.Ax),
            new Op(),
            new Op(),
            new Op("sta", AddressingModes.Zpix),
            new Op(),
            new Op(),
            new Op("sty", AddressingModes.Zp),
            new Op("sta", AddressingModes.Zp),
            new Op("stx", AddressingModes.Zp),
            new Op(),
            new Op("dey", AddressingModes.I),
            new Op(),
            new Op("txa", AddressingModes.I),
            new Op(),
            new Op("sty", AddressingModes.A),
            new Op("sta", AddressingModes.A),
            new Op("stx", AddressingModes.A),
            new Op(),
            new Op("bcc", AddressingModes.R),
            new Op("sta", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op("sty", AddressingModes.Zpx),
            new Op("sta", AddressingModes.Zpx),
            new Op("stx", AddressingModes.Zpy),
            new Op(),
            new Op("tya", AddressingModes.I),
            new Op("sta", AddressingModes.Ay),
            new Op("txs", AddressingModes.I),
            new Op(),
            new Op(),
            new Op("sta", AddressingModes.Ax),
            new Op(),
            new Op(),
            new Op("ldy", AddressingModes.Im),
            new Op("lda", AddressingModes.Zpix),
            new Op("ldx", AddressingModes.Im),
            new Op(),
            new Op("ldy", AddressingModes.Zp),
            new Op("lda", AddressingModes.Zp),
            new Op("ldx", AddressingModes.Zp),
            new Op(),
            new Op("tay", AddressingModes.I),
            new Op("lda", AddressingModes.Im),
            new Op("tax", AddressingModes.I),
            new Op(),
            new Op("ldy", AddressingModes.A),
            new Op("lda", AddressingModes.A),
            new Op("ldx", AddressingModes.A),
            new Op(),
            new Op("bcs", AddressingModes.R),
            new Op("lda", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op("ldy", AddressingModes.Zpx),
            new Op("lda", AddressingModes.Zpx),
            new Op("ldx", AddressingModes.Zpy),
            new Op(),
            new Op("clv", AddressingModes.I),
            new Op("lda", AddressingModes.Ay),
            new Op("tsx", AddressingModes.I),
            new Op(),
            new Op("ldy", AddressingModes.Ax),
            new Op("lda", AddressingModes.Ax),
            new Op("ldx", AddressingModes.Ay),
            new Op(),
            new Op("cpy", AddressingModes.Im),
            new Op("cmp", AddressingModes.Zpix),
            new Op(),
            new Op(),
            new Op("cpy", AddressingModes.Zp),
            new Op("cmp", AddressingModes.Zp),
            new Op("dec", AddressingModes.Zp),
            new Op(),
            new Op("iny", AddressingModes.I),
            new Op("cmp", AddressingModes.Im),
            new Op("dex", AddressingModes.I),
            new Op(),
            new Op("cpy", AddressingModes.A),
            new Op("cmp", AddressingModes.A),
            new Op("dec", AddressingModes.A),
            new Op(),
            new Op("bne", AddressingModes.R),
            new Op("cmp", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op(),
            new Op("cmp", AddressingModes.Zpx),
            new Op("dec", AddressingModes.Zpx),
            new Op(),
            new Op("cld", AddressingModes.I),
            new Op("cmp", AddressingModes.Ay),
            new Op(),
            new Op(),
            new Op(),
            new Op("cmp", AddressingModes.Ax),
            new Op("dec", AddressingModes.Ax),
            new Op(),
            new Op("cpx", AddressingModes.Im),
            new Op("sbc", AddressingModes.Zpix),
            new Op(),
            new Op(),
            new Op("cpx", AddressingModes.Zp),
            new Op("sbc", AddressingModes.Zp),
            new Op("inc", AddressingModes.Zp),
            new Op(),
            new Op("inx", AddressingModes.I),
            new Op("sbc", AddressingModes.Im),
            new Op("nop", AddressingModes.I),
            new Op(),
            new Op("cpx", AddressingModes.A),
            new Op("sbc", AddressingModes.A),
            new Op("inc", AddressingModes.A),
            new Op(),
            new Op("beq", AddressingModes.R),
            new Op("sbc", AddressingModes.Zpiy),
            new Op(),
            new Op(),
            new Op(),
            new Op("sbc", AddressingModes.Zpx),
            new Op("inc", AddressingModes.Zpx),
            new Op(),
            new Op("sed", AddressingModes.I),
            new Op("sbc", AddressingModes.Ay),
            new Op(),
            new Op(),
            new Op(),
            new Op("sbc", AddressingModes.Ax),
            new Op("inc", AddressingModes.Ax),
            new Op(),
        };

        /// <summary>
        /// Symbols used in the program
        /// </summary>
        public Dictionary<int, string> UsedSymbols = new Dictionary<int, string>();

        /// <summary>
        /// Symbols used in the program
        /// </summary>
        public Dictionary<int, string> LocalSymbols = new Dictionary<int, string>();

        /// <summary>
        /// Symbols used in the program
        /// </summary>
        public Dictionary<int, string> UsedLocalSymbols = new Dictionary<int, string>();

        /// <summary>
        /// Determines if symbol is within the program if external
        /// </summary>
        /// <param name="addr">address of symbol</param>
        /// <returns>true if symbol is local</returns>
        private bool IsSymLocal(int addr)
        {
            return addr >= LoadAddress && addr <= LoadAddress + Data.Length - 2;
        }

        /// <summary>
        /// Build the local symbols
        /// The label will be the line number
        /// </summary>
        private void BuildLocalSymbols()
        {
            LocalSymbols.Clear();
            UsedLocalSymbols.Clear();
            var index = 0;
            foreach (var assemblerLine in AssemblerLineCollection)
            {
                LocalSymbols.Add(assemblerLine.Address, $"L_{index++:D4}");
            }
        }

        /// <summary>
        /// Get symbol for an address
        /// </summary>
        /// <param name="symAddress">address of symbol</param>
        /// <param name="len">length of address (1 for page zero 2 for all others</param>
        /// <returns>symbol</returns>
        private string GetSymCommon(int symAddress, int len)
        {
            const int symRange = 3;

            var sym = len == 1 ? $"${symAddress.ToHex()}" : $"${symAddress.ToHexWord()}";
            if (IsSymLocal(symAddress))
            {
                for (var range = 0; range < symRange; ++range)
                {
                    if (!LocalSymbols.TryGetValue(symAddress - range, out var tempSym))
                    {
                        continue;
                    }

                    sym = tempSym;
                    if (!UsedLocalSymbols.ContainsKey(symAddress - range))
                    {
                        UsedLocalSymbols.Add(symAddress - range, sym);
                    }
                    if (range != 0)
                    {
                        sym = $"{sym} + {range}";
                    }

                    return sym;
                }
            }
            else
            {
                for (var range = 0; range < symRange; ++range)
                {
                    if (!_builtInSymbols.TryGetValue(symAddress - range, out var tempSym))
                    {
                        continue;
                    }

                    sym = tempSym;
                    if (!UsedSymbols.ContainsKey(symAddress - range))
                    {
                        UsedSymbols.Add(symAddress - range, sym);
                    }

                    if (range != 0)
                    {
                        sym = $"{sym} + {range}";
                    }

                    return sym;
                }
            }

            return sym;
        }

        /// <summary>
        /// Get a 2 byte symbol
        /// </summary>
        /// <param name="symAddress">2 byte address</param>
        /// <returns>symbol for address</returns>
        private string GetWordSym(int symAddress)
        {
            return GetSymCommon(symAddress, 2);
        }

        /// <summary>
        /// Get a 1 byte symbol
        /// </summary>
        /// <param name="symAddress">1 byte adddress</param>
        /// <returns>symbol for address</returns>
        private string GetByteSym(int symAddress)
        {
            return GetSymCommon(symAddress, 1);
        }

        /// <summary>
        /// Format an opcode
        /// </summary>
        /// <param name="op">Opcode to format</param>
        /// <param name="offset">offset of Data to format</param>
        /// <returns>formatted opcode</returns>
        private string FormatOpCode(Op op, int offset)
        {
            var str = $"{op.Opcode.ToUpperInvariant()} ";
            var symAddress = offset + 2 < Data.Length
                ? Data[offset + 1] + Data[offset + 2] * 256
                : -1;

            var pgZeroSymAddress = offset + 1 < Data.Length
                ? Data[offset + 1]
                : -1;

            string sym;
            int target;
            int d;
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (op.Mode)
            {
                case AddressingModes.I:
                    break;

                case AddressingModes.Im:
                    sym = $"${pgZeroSymAddress.ToHex()}";
                    str += $"#{sym}";
                    break;

                case AddressingModes.Zp:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"{sym}";
                    break;

                case AddressingModes.Zpi:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"({sym})";
                    break;

                case AddressingModes.Zpx:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"{sym},x";
                    break;

                case AddressingModes.Zpy:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"{sym},y";
                    break;

                case AddressingModes.Zpix:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"({sym},x)";
                    break;

                case AddressingModes.Zpiy:
                    sym = GetByteSym(pgZeroSymAddress);
                    str += $"({sym}),y";
                    break;

                case AddressingModes.A:
                    sym = GetWordSym(symAddress);
                    str += sym;
                    break;

                case AddressingModes.Aix:
                    sym = GetWordSym(symAddress);
                    str += $"({sym},x)";
                    break;

                case AddressingModes.Ax:
                    sym = GetWordSym(symAddress);
                    str += $"{sym},x";
                    break;

                case AddressingModes.Ay:
                    sym = GetWordSym(symAddress);
                    str += $"{sym},y";
                    break;

                case AddressingModes.Ind:
                    sym = GetWordSym(symAddress);
                    str += $"({sym})";
                    break;

                case AddressingModes.R:
                    d = (Data[offset + 1] & 0x80) == 0x80
                        ? (-1 & ~0xFF) | Data[offset + 1]
                        : Data[offset + 1];
                    target = LoadAddress + offset + d;
                    sym = GetWordSym(target);
                    str += sym;
                    break;

                case AddressingModes.Zr:
                    d = (Data[offset + 1] & 0x80) == 0x80
                        ? (-1 & ~0xFF) | Data[offset + 1]
                        : Data[offset + 1];
                    target = LoadAddress + offset + d;
                    sym = GetWordSym(target);
                    str += sym;
                    break;

                case AddressingModes.Ac:
                    break;

                case AddressingModes.MaxAddressingMode:
                    break;
            }

            return str;
        }

        /// <summary>
        /// Determine if a byte is an opcode
        /// </summary>
        /// <param name="offset">offst into Data of byte</param>
        /// <returns>true if byte is an opcode</returns>
        public bool IsOpCode(int offset)
        {
            var op = Ops[Data[offset]];
            return !string.IsNullOrEmpty(op.Opcode);
        }

        /// <summary>
        /// Format bytes at offset to the given format type if possible
        /// </summary>
        /// <param name="offset">offset in data</param>
        /// <param name="wantType">desired format type</param>
        /// <returns>AssemblerLine of the data</returns>
        public AssemblerLine BuildOpCode(int offset, AssemblerLine.FormatType wantType)
        {
            var address = LoadAddress + offset - 2;
            int sz;
            string opCode;
            string bytes;
            if (wantType == AssemblerLine.FormatType.Opcode && IsOpCode(offset))
            {
                var op = Ops[Data[offset]];
                sz = GetSize(op.Mode);
                var index = 0;
                if (sz + offset < Data.Length)
                {
                    bytes = "";
                    for (var len = 0; len < sz; ++len)
                    {
                        bytes += $"${Data[offset + index++].ToHex()} ";
                    }

                    bytes = bytes.Trim();
                    opCode = FormatOpCode(Ops[Data[offset]], offset);
                    var line = new AssemblerLine(address, bytes, opCode, AssemblerLine.FormatType.Opcode, sz);
                    line.PropertyChanged += LineOnPropertyChanged;
                    return line;
                }

                sz = 1;
            }
            else
            {
                sz = wantType == AssemblerLine.FormatType.Word ? 2 : 1;
            }

            while (offset + sz > Data.Length)
            {
                --sz;
            }
            if (sz == 0)
            {
                return null;
            }
            bytes = "";
            for (var len = 0; len < sz; ++len)
            {
                bytes += $"${Data[offset + len].ToHex()} ";
            }

            bytes = bytes.Trim();

            int addr;
            string sym;
            string directive;
            if (sz == 1)
            {
                addr = Data[offset];
                directive = ".BYTE";
                sym = $"${addr.ToHex()}";
            }
            else
            {
                addr = Data[offset] + Data[offset + 1] * 256;
                directive = ".WORD";
                sym = $"${addr.ToHexWord()}";
            }
            if (LocalSymbols.TryGetValue(addr, out var tempSym))
            {
                sym = tempSym;
                if (!UsedLocalSymbols.ContainsKey(addr))
                {
                    UsedLocalSymbols.Add(addr, sym);
                }
            }
            opCode = $"{directive} {sym}";

            var dataLine = new AssemblerLine(address, bytes, opCode,
                sz == 1 ? AssemblerLine.FormatType.Byte : AssemblerLine.FormatType.Word, sz);
            dataLine.PropertyChanged += LineOnPropertyChanged;
            return dataLine;
        }

        /// <summary>
        /// Build all the Assembler lines
        /// </summary>
        private void BuildAssemblerLines()
        {
            ReBuilding = true;

            UsedSymbols.Clear();
            UsedLocalSymbols.Clear();
            AssemblerLineCollection.Clear();

            var offset = 0;
            LoadAddress = Data[0] + Data[1] * 256;
            offset += 2;
            var index = 0;
            while (offset < Data.Length)
            {
                var line = BuildOpCode(offset, AssemblerLine.FormatType.Opcode);
                if (line == null)
                {
                    _ = MessageBox.Show("Failed to disassemble");
                    return;
                }

                offset += line.Size;
                line.RowIndex = index++;
                AssemblerLineCollection.Add(line);
            }

            ReBuilding = false;

            SyncRowsLabels();

            ValidateCollection();
        }

        /// <summary>
        /// Reset the Assembler indexes
        /// </summary>
        private void ResetIndexes()
        {
            var addr = LoadAddress;
            for (var r = 0; r < AssemblerLineCollection.Count; ++r)
            {
                AssemblerLineCollection[r].RowIndex = r;
                AssemblerLineCollection[r].Address = addr;
                AssemblerLineCollection[r].Label = "";
                addr += AssemblerLineCollection[r].Size;
            }
        }

        /// <summary>
        /// sync symbols and indexes for labels
        /// must be called if there is any change to an assembler line
        /// </summary>
        private void SyncRowsLabels()
        {
            if (ReBuilding)
            {
                return;
            }

            var index = 0;
            LocalSymbols.Clear();
            UsedLocalSymbols.Clear();
            ResetIndexes();
            BuildLocalSymbols();

            // Copy the current lines
            var temp = new AssemblerLine[AssemblerLineCollection.Count];
            AssemblerLineCollection.CopyTo(temp, 0);
            AssemblerLineCollection.Clear();

            // Rebuild lines with possible new lables
            foreach (var oldLine in temp)
            {
                var line = BuildOpCode(oldLine.Address - LoadAddress + 2, (AssemblerLine.FormatType)oldLine.Format);
                if (line == null)
                {
                    _ = MessageBox.Show("Failed to disassemble");
                    return;
                }

                line.RowIndex = index++;
                AssemblerLineCollection.Add(line);
            }

            // Now add the left column label for the used labels
            foreach (var assemblerLine in AssemblerLineCollection)
            {
                if (!UsedLocalSymbols.ContainsKey(assemblerLine.Address))
                {
                    continue;
                }

                if (LocalSymbols.TryGetValue(assemblerLine.Address, out var sym))
                {
                    assemblerLine.Label = sym;
                }
            }

            // build the external labels
            SymCollection.Clear();
            foreach (var usedSymbolsKey in UsedSymbols.Keys.OrderBy(key => key))
            {
                if (UsedSymbols.TryGetValue(usedSymbolsKey, out var val))
                {
                    SymCollection.Add((usedSymbolsKey & 0xFF00) != 0
                        ? $"{string.Empty,-10}{val} = ${usedSymbolsKey.ToHexWord()}"
                        : $"{string.Empty,-10}{val} = ${usedSymbolsKey.ToHex()}");
                }
            }

            // add blank line and org
            SymCollection.Add("");
            SymCollection.Add($"{string.Empty,-10}.ORG ${LoadAddress.ToHexWord()}");
        }

        /// <summary>
        /// Sanity check Assembler lines
        /// </summary>
        [Conditional("DEBUG")]
        public void ValidateCollection()
        {
            var lastLine = -1;
            var address = LoadAddress;
            foreach (var assemblerLine in AssemblerLineCollection)
            {
                if (assemblerLine.RowIndex != lastLine + 1)
                {
                    _ = MessageBox.Show($"Index out of sync ROW {assemblerLine.RowIndex}  should be {lastLine + 1}.\n" +
                                        $"{assemblerLine.Label} {assemblerLine.OpCodes} {assemblerLine.Comment}");
                    return;
                }
                if (address != assemblerLine.Address)
                {
                    _ = MessageBox.Show($"Address out of sync ROW {assemblerLine.RowIndex}.\n" +
                                        $"{assemblerLine.Label} {assemblerLine.OpCodes} {assemblerLine.Comment}");
                    return;
                }

                address += assemblerLine.Size;
                lastLine = assemblerLine.RowIndex;
            }
        }

        /// <summary>
        /// Reformat a line
        /// </summary>
        /// <param name="line">line to format</param>
        /// <param name="format">new format</param>
        public void FormatLine(AssemblerLine line, AssemblerLine.FormatType format)
        {
            var oldSize = line.Size;
            var newOffset = line.Address - LoadAddress + 2;
            var newLine = BuildOpCode(newOffset, format);

            newLine.RowIndex = line.RowIndex;
            AssemblerLineCollection.RemoveAt(line.RowIndex);
            AssemblerLineCollection.Insert(newLine.RowIndex, newLine);

            var bytesToInsert = 0;
            var index = newLine.RowIndex;
            if (oldSize > newLine.Size)
            {
                bytesToInsert = oldSize - newLine.Size;
                newOffset += newLine.Size;
            }
            else if (newLine.Size > oldSize)
            {
                var delIndex = newLine.RowIndex + 1;
                var n = AssemblerLineCollection[delIndex].Size;
                AssemblerLineCollection.RemoveAt(delIndex);

                var w = newLine.Size - oldSize;
                bytesToInsert = n - w;
                newOffset += newLine.Size;
            }

            while (bytesToInsert > 0)
            {
                var insertLine = BuildOpCode(newOffset, AssemblerLine.FormatType.Byte);
                insertLine.RowIndex = ++index;
                AssemblerLineCollection.Insert(insertLine.RowIndex, insertLine);
                bytesToInsert -= insertLine.Size;
                newOffset += insertLine.Size;
            }

            SyncRowsLabels();
        }

        private ObservableCollection<AssemblerLine> _assemblerLineCollection;

        /// <summary>
        /// Collection of assembled lines (backing for GUI)
        /// </summary>
        public ObservableCollection<AssemblerLine> AssemblerLineCollection
        {
            get => _assemblerLineCollection;
            set
            {
                _assemblerLineCollection = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Constructor Create collection of lines
        /// </summary>
        public ViewModel()
        {
            AssemblerLineCollection = new ObservableCollection<AssemblerLine>();
            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// A property has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, "LoadAdddress",
                StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Org = $".ORG ${LoadAddress.ToHexWord()}";
            }
            else if (string.Compare(e.PropertyName, "Data", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                BuildAssemblerLines();
            }
        }

        /// <summary>
        /// A line has changed
        /// </summary>
        /// <param name="sender">line being changed</param>
        /// <param name="e">parameters chamged</param>
        private void LineOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, "Format", StringComparison.CurrentCultureIgnoreCase) != 0)
            {
                return;
            }

            var line = (AssemblerLine) sender;
            FormatLine(line, (AssemblerLine.FormatType) line.Format);
        }
    }
}
