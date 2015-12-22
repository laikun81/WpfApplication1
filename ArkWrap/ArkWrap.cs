using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace ArkWrap
{
    public abstract class SingletonBase<T> where T : SingletonBase<T>
    {
        #region Members

        /// <summary>
        /// Static instance. Needs to use lambda expression
        /// to construct an instance (since constructor is private).
        /// </summary>
        private static readonly Lazy<T> sInstance = new Lazy<T>(() => CreateInstanceOfT());

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance of this singleton.
        /// </summary>
        public static T Instance { get { return sInstance.Value; } }

        #endregion

        #region Methods

        /// <summary>
        /// Creates an instance of T via reflection since T's constructor is expected to be private.
        /// </summary>
        /// <returns></returns>
        private static T CreateInstanceOfT()
        {
            return Activator.CreateInstance(typeof(T), true) as T;
        }

        #endregion
    }

    public class Ark : SingletonBase<Ark>
    {
        private Ark() { }

        #region struct
        //#define ARK_FILESIZE_UNKNOWN			(0xffffffffffffffffLL)	// 파일 크기를 알 수 없을때 사용되는 값
        public const ulong ARK_FILESIZE_UNKNOWN = 0xffffffffffffffffL;

        public const uint ARK_FILEATTR_NONE = 0x00;
        public const uint ARK_FILEATTR_READONLY = 0x01;	// FILE_ATTRIBUTE_READONLY
        public const uint ARK_FILEATTR_HIDDEN = 0x02;	// FILE_ATTRIBUTE_HIDDEN
        public const uint ARK_FILEATTR_SYSTEM = 0x04;	// FILE_ATTRIBUTE_SYSTEM
        public const uint ARK_FILEATTR_DIRECTORY = 0x10;	// FILE_ATTRIBUTE_DIRECTORY
        public const uint ARK_FILEATTR_FILE = 0x20;	// FILE_ATTRIBUTE_ARCHIVE

        /// <summary>
        /// 압축 및 압축 해제 진행 상황 정보
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        public struct SArkProgressInfo
        {
            /// <summary>
            /// 현재 파일의 압축 해제 진행율(%)
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float fCurPercent;
            /// <summary>
            /// 전체 파일의 압축 해제 진행율(%)
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float fTotPercent;
            /// <summary>
            /// 마무리 중인가?
            /// </summary>
            [MarshalAs(UnmanagedType.Bool)]
            public bool bCompleting;
            /// <summary>
            /// 마무리 중일때 진행율(%)
            /// </summary>
            [MarshalAs(UnmanagedType.R4)]
            public float fCompletingPercent;
            /// <summary>
            /// undocumented - do not use
            /// </summary>
            public int _processed;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SArkFileItem
        {
            public string Filename
            {
                get
                {
                    return Marshal.PtrToStringAuto(fileNameW) ?? Marshal.PtrToStringUni(fileNameW);
                }
            }
            public static SArkFileItem PtrToItem(IntPtr handle)
            {
                return (Ark.SArkFileItem)Marshal.PtrToStructure(handle , typeof(Ark.SArkFileItem));
            }
            // 압축파일에 저장된 파일명 (이 이름은 폴더 경로명도 포함한다)
            //[MarshalAs(UnmanagedType.LPStr)]
            //public string fileName;
            public IntPtr fileName;
            //[MarshalAs(UnmanagedType.LPWStr)]
            //public string fileNameW;
            public IntPtr fileNameW;
            //[MarshalAs(UnmanagedType.LPWStr)]
            //public string fileCommentW;
            public IntPtr fileCommentW;
            //ARK_TIME_T(INT64)						fileTime;					// last modified(write) time
            public readonly long fileTime;
            //INT64					uncompressedSize;
            public readonly long compressedSize;
            //INT64					uncompressedSize;
            public readonly long uncompressedSize;

            //ARK_ENCRYPTION_METHOD	encryptionMethod;
            public readonly ARK_ENCRYPTION_METHOD encryptionMethod;
            //ARK_FILEATTR			attrib;
            public readonly uint attrib;
            //UINT32					crc32;
            public readonly uint crc32;
            //ARK_COMPRESSION_METHOD	compressionMethod;
            public readonly ARK_COMPRESSION_METHOD compressionMethod;

            // NTFS 파일 시간 정보. 압축파일에 NTFS 파일정보가 없으면 NULL임
            //SArkNtfsFileTimes*		ntfsFileTimes;				
            [MarshalAs(UnmanagedType.Struct)]
            public readonly SArkNtfsFileTimes ntfsFileTimes;

            // 유니코드를 지원하는 압축포맷을 통해서 가져온 파일 이름인가? (즉, fileNameW를 100% 믿을 수 있는가)
            //BOOL32					isUnicodeFileName;			
            [MarshalAs(UnmanagedType.Bool)]
            public readonly bool isUnicodeFileName;
            //BOOL32					IsFolder() const { return attrib & ARK_FILEATTR_DIRECTORY ? TRUE : FALSE;}
            //[MarshalAs(UnmanagedType.Bool)]
            public bool IsFolder() { return (attrib & ARK_FILEATTR_DIRECTORY) == ARK_FILEATTR_DIRECTORY; }
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SArkNtfsFileTimes							// NTSF 파일 시간 정보
        {
            [MarshalAs(UnmanagedType.Struct)]
            public SArkFileTime mtime;							// 마지막 수정 시간
            [MarshalAs(UnmanagedType.Struct)]
            public SArkFileTime ctime;							// 파일을 생성한 시간
            [MarshalAs(UnmanagedType.Struct)]
            public SArkFileTime atime;							// 마지막으로 파일에 접근한 시간
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SArkFileTime									// FILETIME(ntfs)과 동일
        {
            //UINT32 dwLowDateTime;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwLowDateTime;
            //UINT32 dwHighDateTime;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwHighDateTime;
        };

        // 에러코드
        public enum ARKERR
        {
            _NOERR = 0x00,
            _CANT_OPEN_FILE = 0x01,		// 파일 열기 실패
            _CANT_READ_SIG = 0x02,		// signature 읽기 실패
            _AT_READ_CONTAINER_HEADER = 0x03,		// 컨테이너 헤더가 손상되었음
            _INVALID_FILENAME_LENGTH = 0x04,		// 파일명 길이에 문제
            _READ_FILE_NAME_FAILED = 0x05,		// 파일이름 읽기 실패
            _INVALID_EXTRAFIELD_LENGTH = 0x06,		// extra field 읽기
            _READ_EXTRAFILED_FAILED = 0x07,		// extra field 읽기 실패
            _CANT_READ_CENTRAL_DIRECTORY_STRUCTURE = 0x08,		// (zip) Central Directory 정보를 읽는데 실패하였음
            _INVALID_FILENAME_SIZE = 0x09,		// 파일명 길이 정보가 잘못되었음
            _INVALID_EXTRAFIELD_SIZE = 0x10,		// (zip) ExtraField 정보 길이가 잘못되었음
            _INVALID_FILECOMMENT_SIZE = 0x11,		// Comment 정보 길이가 잘못되었음
            _CANT_READ_CONTAINER_HEADER = 0x12,		// 컨테이너의 헤더에 문제가 있음
            _MEM_ALLOC_FAILED = 0x13,		// 메모리 할당 실패
            _CANT_READ_DATA = 0x15,		// 압축 데이타 읽기 실패
            _INFLATE_FAILED = 0x16,		// Inflate 함수 호출중 에러 발생
            _USER_ABORTED = 0x17,		// 사용자 중지
            _INVALID_FILE_CRC = 0x18,		// 압축 해제후 CRC 에러 발생
            _UNKNOWN_COMPRESSION_METHOD = 0x19,		// 모르는(혹은 지원하지 않는) 압축방식
            _PASSWD_NOT_SET = 0x20,		// 암호걸린 파일인데 암호가 지정되지 않았음
            _INVALID_PASSWD = 0x21,		// 암호가 틀렸음
            _WRITE_FAIL = 0x30,		// 파일 쓰다가 실패
            _CANT_OPEN_DEST_FILE = 0x31,		// 대상 파일을 만들 수 없음
            _BZIP2_ERROR = 0x32,		// BZIP2 압축해제중 에러 발생
            _INVALID_DEST_PATH = 0x33,		// 경로명에 ../ 이 포함된 경우, 대상 경로에 접근이 불가능한 경우
            _CANT_CREATE_FOLDER = 0x34,		// 경로 생성 실패
            _DATA_CORRUPTED = 0x35,		// 압축푸는데 데이타가 손상됨 or RAR 분할 압축파일의 뒷부분이 없음
            _CANT_OPEN_FILE_TO_WRITE = 0x36,		// 쓰기용으로 파일 열기 실패
            _INVALID_INDEX = 0x37,		// 압축풀 대상의 index 파라메터가 잘못됨
            _CANT_READ_CODEC_HEADER = 0x38,		// 압축 코덱의 헤더를 읽는데 에러
            _CANT_INITIALIZE_CODEC = 0x39,		// 코덱 초기화 실패
            _LZMA_ERROR = 0x40,		// LZMA 압축 해제중 에러 발생
            _PPMD_ERROR = 0x41,		// ppmd 에러
            _CANT_SET_OUT_FILE_SIZE = 0x42,		// 출력파일의 SetSize() 실패
            _NOT_MATCH_FILE_SIZE = 0x43,		// 압축을 푼 파일 크기가 맞지 않음
            _NOT_A_FIRST_VOLUME_FILE = 0x44,		// 분할 압축파일중 첫번째 파일이 아님
            _NOT_OPENED = 0x45,		// 파일이 열려있지 않음
            _NOT_SUPPORTED_ENCRYPTION_METHOD = 0x46,		// 지원하지 않는 암호 방식
            _INTERNAL = 0x47,		// 내부 에러
            _NOT_SUPPORTED_FILEFORMAT = 0x48,		// 지원하지 않는 파일 포맷
            _UNKNOWN_FILEFORMAT = 0x49,		// 압축파일이 아님
            _FILENAME_EXCED_RANGE = 0x50,		// 경로명이 너무 길어서 파일이나 폴더를 만들 수 없음
            _LZ_ERROR = 0x52,		// lz 에러
            _NOTIMPL = 0x53,		// not implemented
            _DISK_FULL = 0x54,		// 파일 쓰다가 실패
            _FILE_TRUNCATED = 0x55,		// 파일의 뒷부분이 잘렸음
            _CANT_DO_THAT_WHILE_WORKING = 0x56,		// 압축 해제 작업중에는 파일을 열거나 닫을 수 없음
            _CANNOT_FIND_NEXT_VOLUME = 0x57,		// 분할 압축된 파일의 다음 파일을 찾을 수 없음
            _NOT_ARCHIVE_FILE = 0x58,		// 압축파일이 아님 (Open() 호출시 명백히 압축파일이 아닌 경우 발생)
            _USER_SKIP = 0x59,		// 사용자가 건너띄기 했음.
            _INVALID_PASSWD_OR_BROKEN_ARCHIVE = 0x60,		// 암호가 틀리거나 파일이 손상되었음 (rar 포맷)
            _ZIP_LAST_VOL_ONLY = 0x61,		// 분할 zip 인데 마지막 zip 파일만 열려고 했음
            _ACCESS_DENIED_TO_DEST_PATH = 0x62,		// 대상 폴더에 대해서 쓰기 권한이 없음
            _NOT_ENOUGH_MEMORY = 0x63,		// 메모리가 부족함
            _NOT_ENOUGH_MEMORY_LZMA_ENCODE = 0x64,		// LZMA 압축중 메모리가 부족함
            _CANT_OPEN_SHARING_VIOLATION = 0x65,		// 파일이 잠겨있어서 열 수 없음 (ERROR_SHARING_VIOLATION, WIN32 전용)
            _CANT_OPEN_ERROR_LOCK_VIOLATION = 0x66,		// 파일이 잠겨있어서 열 수 없음 (ERROR_LOCK_VIOLATION, WIN32 전용)
            _CANT_LOAD_UNACE = 0x67,		// unace32.exe 혹은 unacev2.dll 파일을 로드할 수 없음 (WIN32 전용)
            _NOT_SUPPORTED_OPERATION = 0x68,		// 지원하지 않는 작동입니다. (ACE 파일을 IArkSimpleOutStream 를 이용해 압축해제할 경우 발생)
            _CANT_CONVERT_FILENAME = 0x69,		// 파일명이 잘못되어서 유니코드 파일명으로 바꿀 수 없음(posix 환경에서 iconv 사용시 코드페이지가 잘못된 경우 사용할 수 없는 문자 때문에 발생)
            _TOO_LONG_FILE_NAME = 0x70,		// 파일명이 너무 길어서 처리할 수 없음
            _TOO_LONG_FILE_NAME_AND_TRUNCATED = 0x71,		// 파일명이 너무 길어서 뒷부분이 잘렸습니다.
            _TOO_MANY_FILE_COUNT = 0x72,		// 파일 갯수가 너무 길어서 처리할 수 없음
            _CANT_OPEN_SRC_FILE_TO_COPY = 0x73,		// 파일을 복사하기 위한 원본 파일을 열 수 없음 (rar5 redirect 처리용)

            _CORRUPTED_FILE = 0x100,	// 파일이 손상되었음
            _INVALID_FILE = 0x101,	// 포맷이 다르다
            _CANT_READ_FILE = 0x102,	// 파일을 읽을 수 없음

            _INVALID_VERSION = 0x200,	// 헤더파일과 dll 의 버전이 맞지 않음
            _ENCRYPTED_BOND_FILE = 0x201,	// 압축 해제 불가(암호화된 bond 파일임)

            _7ZERR_BROKEN_ARCHIVE = 0x300,	// 7z.dll 으로 열때 에러가 발생(깨진파일)
            _LOAD_7Z_DLL_FAILED = 0x301,	// 7z.dll 열다가 에러 발생

            _CANT_CREATE_FILE = 0x401,	// 파일을 쓰기용으로 생성하지 못함
            _INIT_NOT_CALLED = 0x402,	// Init() 함수가 호출되지 않았음
            _INVALID_PARAM = 0x403,	// 잘못된 파라메터로 호출하였음
            _CANT_OPEN_INPUT_SFX = 0x404,	// SFX 파일을 열지 못함
            _SFX_SIZE_OVER_4GB = 0x405,	// SFX 파일의 크기가 4GB를 넘었음
            _CANT_LOAD_ARKLGPL = 0x406,	// ArkXXLgpl.dll 파일을 열지 못함
            _CANT_STORE_FILE_SIZE_OVER_4GB = 0x407,	// 파일 크기가 4GB를 넘어서 저장할 수 없음

            _ALREADY_DLL_CREATED = 0x902,	// (CArkLib) 이미 ARK DLL 파일을 로드하였음
            _LOADLIBRARY_FAILED = 0x903,	// (CArkLib) LoadLibrary() 호출 실패
            _GETPROCADDRESS_FAILED = 0x904,	// (CArkLib) GetProcAddress() 호출 실패
            _UNSUPPORTED_OS = 0x905,	// (CArkLib) 지원하지 않는 os 
            _LIBRARY_NOT_LOADED = 0x906,	// (CArkLib) 라이브러리를 로드하지 않았거나 로드하는데 실패하였음
        };

        // ARK FILE FORMAT
        public enum ARK_FF
        {
            _ZIP,								// zip, zipx
            _ZIP_LASTVOLONLY,					// 분할 zip 파일의 마지막 볼륨 (파일이 하나만 존재할 경우)
            _ZIP_BANDIZIP_SFX,				// 반디집 sfx 
            _ALZ,
            _ALZ_SECONDVOL,					// 분할 alz 파일의 2번째 이후 압축파일
            _LZH,
            _RAR,
            _RAR5,
            _RAR_SECONDVOL,					// 분할 RAR 파일의 2번째 이후 압축파일
            _7Z,
            _7ZSPLIT,							// 7z 파일의 뒷부분이 잘렸고 확장자가 .001 인 파일 (.7z.001 ~ .7z.NNN)
            _7ZBROKEN,						// 7z 파일의 뒷부분이 잘렸거나 헤더가 손상된 파일
            _TAR,
            _CAB,
            _CAB_NOTFIRSTVOL,					// 
            _ISO,								// iso, joliet
            _IMG,								// clone cd img (img, ccd)
            _UDF,
            _UDFBROKEN,						// 뒷부분이 잘린 UDF 
            _SPLIT,							// 확장자가 .001 인 파일 (.001 ~ .NNN)
            _BOND,							// hv3
            _GZ,
            _BZ2,
            _LZMA,
            _BH,								// blakhole
            _EGG,
            _EGG_NOTFIRSTVOL,					// 분할 압축의 첫번째 볼륨이 아닌 파일
            _XZ,
            _WIM,								// raw 만 사용하는 wim
            _WIM_COMPRESSED,					// 압축된 wim, Windows 에서만 지원
            _FREEARC,							// FreeArc - 파일 목록열기만 지원
            _Z,								// .Z (unix compress)
            _ARJ,								// arj 
            _BAMSFX,							// 밤톨이 sfx
            _BAMSFX_NOTFIRSTVOL,				// 
            _TGZ,								// .tar.gz
            _TBZ,								// .tar.bz2
            _J2J,								// .j2j
            _J2JBROKEN,						// 뒷부분이 잘린 j2j
            _K2K,								// .k2k
            _NSIS,							// nsis exe

            _UNKNOWN = 0x00ff,	// 알 수 없는 파일 포맷

            _UNSUPPORTED_FIRST = 0x0100,	// 지원하지 않는 압축파일 포맷
            _SIT = 0x0100,	// sit
            _BPE = 0x0101,	// bpe
            _ACE = 0x0102,	// ace
            _PAE = 0x0104,	// PowerArchiver Encryption
            _XEF = 0x0105,	// Winace Encryption
            _COMPOUND = 0x0106,	// MSI, XLS, PPT, DOC ...
            _UNSUPPORTED_LAST = 0x01FF,

            _NOTARCHIVE_FIRST = 0x0200,	// 명백히 압축파일이 아닌 파일 (실행파일, 이미지파일 등등..)
            _NULL = 0x0201,	// 파일의 앞부분이 전부 0 으로 채워져 있는 파일
            _RIFF = 0x0202,	// avi, wav
            _EXE = 0x0203,	// sfx 가 아닌 일반 PE 실행파일
            _HTML = 0x0204,	// HTML(정확하지는 않음)
            _JPG = 0x0205,	// 
            _PNG = 0x0206,	// 
            _GIF = 0x0207,	// 
            _OGGS = 0x0208,	// OggS
            _MATROSKA = 0x0209,	// MKV
            _PDF = 0x020a,	// PDF
            _NOTARCHIVE_LAST = 0x020a,

        };

        // 암호화 방식
        public enum ARK_ENCRYPTION_METHOD
        {
            _NONE = 0x00,
            _ZIP = 0x01,	// ZipCrypto
            _AES128 = 0x02,	// zip
            _AES192 = 0x03,
            _AES256 = 0x04,

            _EGG_ZIP = 0x05,	// EGG 포맷에서 사용
            _EGG_AES128 = 0x06,
            _EGG_AES256 = 0x07,

            _RAR = 0x08,	// RAR 암호 방식
            _ACE = 0x09,	// ACE 암호

            _ETC = 0x99,

            _NOTSUPPORTED_FIRST = 0x100,	// Not supported encryption method
            _GARBLE,						// ARJ 암호 방식
            _DES,
            _RC2,
            _3DES168,
            _3DES112,
            _PKAES128,
            _PKAES192,
            _PKAES256,
            _RC2_2,
            _BLOWFISH,
            _TWOFISH,
            _RC4,
            _UNKNOWN,
        };

        // 압축 방식
        public enum ARK_COMPRESSION_METHOD
        {
            /////////////////////////////////////////////////////////////////
            // zip 에서 사용하는것들, zip 포맷에 정의된 값과 동일하다.
            // (http://www.pkware.com/documents/casestudies/APPNOTE.TXT 참고)
            _STORE = 0,
            _SHRINK = 1,
            _IMPLODE = 6,
            _DEFLATE = 8,

            _DEFLATE64 = 9,
            _BZIP2 = 12,
            _LZMA = 14,		// zipx, 7zip ...
            _JPEG = 96,		// zipx
            _WAVPACK = 97,		// zipx
            _PPMD = 98,		// zipx, 7zip
            _AES = 99,		// aes 로 암호화된 zip 파일. 실제 압축 방법은 다른곳에 저장된다.
            // 
            /////////////////////////////////////////////////////////////////

            /////////////////////////////////////////////////////////////////
            // ETC
            _FUSE = 300,	// bh 에서 사용 
            _FUSE6 = 301,	// bh 에서 사용 
            _AZO = 302,	// egg 에서 사용
            _COMPRESS = 303,	// .Z 에서 사용

            _RAR15 = 400,	// RAR 1.5
            _RAR20 = 401,	// RAR 2.X
            _RAR26 = 402,	// RAR 2.X & 2GB 이상
            _RAR29 = 403,	// RAR 3.X
            _RAR36 = 404,	// RAR 3.X alternative hash
            _RAR50 = 405,	// RAR 5.0
            _REDIR = 406,	// Redirect (RAR5)

            _MSZIP = 500,	// CAB
            _LHA = 501,	// lzh
            _LZMA2 = 502,	// 7z
            _BCJ = 503,	// 7z
            _BCJ2 = 504,	// 7z
            _LZX = 505,	// CAB
            _LZXWIM = 506,	// wim
            _OBDEFLATE = 508,	// Obfuscated deflate (alz)
            _DELTA = 509,	// 7z
            _XPRESS = 510,	// wim - xpress

            _LH0 = 600,	// -lh0-
            _LH1 = 601,	// -lh1-
            _LH2 = 602,	// -lh2-
            _LH3 = 603,	// -lh3-
            _LH4 = 604,	// -lh4-
            _LH5 = 605,	// -lh5-
            _LH6 = 606,	// -lh6-
            _LH7 = 607,	// -lh7-
            _LZS = 608,	// -lzs-
            _LZ5 = 609,	// -lz5-
            _LZ4 = 610,	// -lz4-
            _LHD = 611,	// -lhd-
            _PM0 = 612,	// -pm0-
            _PM2 = 613,	// -pm2-

            _LZX15 = 715,	// LZX (WINDOW SIZE 15bit)
            _LZX16 = 716,	// 
            _LZX17 = 717,	// 
            _LZX18 = 718,	// 
            _LZX19 = 719,	// 
            _LZX20 = 720,	// 
            _LZX21 = 721,	// LZX (WINDOW SIZE 21bit)

            _QUANTUM10 = 810,	// QTMD(WINDOW SIZE 10bit)
            _QUANTUM11 = 811,	//
            _QUANTUM12 = 812,	//
            _QUANTUM13 = 813,	//
            _QUANTUM14 = 814,	//
            _QUANTUM15 = 815,	//
            _QUANTUM16 = 816,	//
            _QUANTUM17 = 817,	//
            _QUANTUM18 = 818,	//
            _QUANTUM19 = 819,	//
            _QUANTUM20 = 820,	//
            _QUANTUM21 = 821,	// QTMD(WINDOW SIZE 21bit)

            _ARJ1 = 901,	// Arj Method 1
            _ARJ2 = 902,	//            2
            _ARJ3 = 903,	//            3
            _ARJ4 = 904,	//            4

            _ACELZ77 = 910,	// ace lz77
            _ACE20 = 911,	// ace v20
            _ACE = 912,	// ace 최신?

            // 
            /////////////////////////////////////////////////////////////////

            _UNKNOWN = 9999,	// unknown
        };

        // 분할 압축 스타일
        public enum ARK_MULTIVOL_STYLE
        {
            _NONE,			// 분할 압축파일이 아님
            _001,				// 7zip 의 001, 002, .. 스타일
            _WINZIP,			// winzip 스타일  (z01, z02 ..... zip)
            _ZIPX,			// winzip zipx 스타일  (zx01, zx02 ..... zipx)
            _ALZ,				// alzip 의 alz, a00, a01, a02, .. 스타일
            _EGG,				// vol1.egg vol2.egg vol3.egg ... 스타일
            _RAR,				// part1.rar part2.rar ... 스타일
            _R00,				// .rar .r00 .r01 스타일
            _ARJ,				// .arj .a01 .a02 스타일
            _BAMSFX,			// 밤톨이 sfx (exe, .002 .003 ...)
            _BDZSFX,			// 반디집 SFX (exe, .e01 .e02 ...)
            _CAB,				// 분할 cab 파일
        };

        /// <summary>
        /// 파일 덮어쓰기 질문에 대한 사용자 대답
        /// </summary>
        public enum ARK_OVERWRITE_MODE
        {
            OVERWRITE,
            SKIP,
            RENAME,
        };
        /// <summary>
        /// 파일 암호 질문에 대한 사용자 대답
        /// </summary>
        public enum ARK_PASSWORD_RET
        {
            OK,
            CANCEL,
        };
        /// <summary>
        /// OnAskPassword() 호출 이유
        /// </summary>
        public enum ARK_PASSWORD_ASKTYPE
        {
            /// <summary>
            /// 암호가 지정되지 않았음
            /// </summary>
            PASSWDNOTSET,
            /// <summary>
            /// 기존에 지정된 암호가 틀렸음
            /// </summary>
            INVALIDPASSWD,
        };

        /////////////////////////////////////////////////////////
        //
        // 압축 옵션
        //
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SArkCompressorOpt
        {
            SArkCompressorOpt NewOpt()
            {
                SArkCompressorOpt opt = new SArkCompressorOpt();
                opt.ff = ARK_FF._ZIP;
                opt.saveNTFSTime = false;
                opt.streamOutput = false;
                opt.compressionMethod = ARK_COMPRESSION_METHOD._DEFLATE;
                opt.encryptionMethod = ARK_ENCRYPTION_METHOD._ZIP;
                opt.compressionLevel = -1;							// -1 은 Z_DEFAULT_COMPRESSION
                opt.splitSize = 0;
                opt.forceZip64 = false;
                opt.useDosTime2PasswordCheck = true;
                opt.sfxPathName = null;
                opt.forceUtf8FileName = false;
                opt.forceUtf8Comment = false;
                opt.utf8FileNameIfNeeded = true;
                opt.bypassWhenUncompressible = false;
                opt.lzmaEncodeThreadCount = 2;
                opt.enableMultithreadDeflate = false;
                opt.deflateEncodeThreadCount = 0;
                opt._7zCompressHeader = true;
                opt._7zEncryptHeader = false;
                opt.lzma2NumBlockThreads = -1;
                opt.threadPriority = 0;
                opt.deleteArchiveWhenFailed = true;
                return opt;
            }
            /// <summary>
            /// 파일 포맷 ARK_FF_ZIP, ARK_FF_TAR, ARK_FF_TGZ, ARK_FF_7Z, ARK_FF_LZH, ARK_FF_ISO
            /// </summary>
            ARK_FF ff;
            /// <summary>
            /// ntfs 시간 저장 여부
            /// </summary>
            [MarshalAs(UnmanagedType.Bool)]
            bool saveNTFSTime;
            /// <summary>
            /// stream 형태로 저장 - 이 값이 TRUE 일 경우 열지 못하는 프로그램이 너무 많음.
            /// </summary>
            [MarshalAs(UnmanagedType.Bool)]
            bool streamOutput;
            /// <summary>
            /// 압축 방식 ( ARK_COMPRESSION_METHOD_STORE, ARK_COMPRESSION_METHOD_DEFLATE, ARK_COMPRESSION_METHOD_LZMA )
            /// </summary>
            ARK_COMPRESSION_METHOD compressionMethod;
            ARK_ENCRYPTION_METHOD encryptionMethod;	// 파일에 암호를 걸 경우 사용할 암호 방식 ( ARK_ENCRYPTION_METHOD_ZIP, ARK_ENCRYPTION_METHOD_AES256 만 유효)
            int compressionLevel;			// 압축 레벨 ( Z_NO_COMPRESSION, Z_BEST_SPEED ~ Z_BEST_COMPRESSION )
            [MarshalAs(UnmanagedType.I8)]
            long splitSize;					// 분할 압축 크기 (bytes,  0 이면 분할 압축 안함)
            bool forceZip64;					// 강제로 zip64 정보 저장
            [MarshalAs(UnmanagedType.Bool)]
            bool useDosTime2PasswordCheck;	// 암호 체크 데이타를 crc 대신 dostime 을 사용한다. (사용시 압축 속도 향상). 단 분할압축시 이 옵션은 무시됨
            [MarshalAs(UnmanagedType.LPWStr)]
            string sfxPathName;				// sfx를 만들경우 sfx 파일경로명. NULL 이면 사용하지 않음.
            [MarshalAs(UnmanagedType.Bool)]
            bool forceUtf8FileName;			// 파일명을 모두 utf8 로 저장
            [MarshalAs(UnmanagedType.Bool)]
            bool forceUtf8Comment;			// 압축파일 설명을 utf8 로 저장 (다른 프로그램과 호완되지 않음)
            [MarshalAs(UnmanagedType.Bool)]
            bool utf8FileNameIfNeeded;		// 파일명에 유니코드가 포함되어 있을 경우 utf8 로 저장
            [MarshalAs(UnmanagedType.Bool)]
            bool bypassWhenUncompressible;	// 압축중 압축이 안될경우 그냥 bypass
            int lzmaEncodeThreadCount;		// LZMA 압축시 쓰레드 카운트. 1~2
            [MarshalAs(UnmanagedType.Bool)]
            bool enableMultithreadDeflate;	// Deflate 압축시 멀티쓰레드 사용
            int deflateEncodeThreadCount;	// Deflate 압축시 사용할 쓰래드 갯수. 0 이면 기본값 사용
            [MarshalAs(UnmanagedType.Bool)]
            bool deleteArchiveWhenFailed;	// 압축중 에러 발생시 생성된 파일 삭제하기

            [MarshalAs(UnmanagedType.Bool)]
            bool _7zCompressHeader;			// 7zip 압축시 헤더를 압축할 것인가?
            [MarshalAs(UnmanagedType.Bool)]
            bool _7zEncryptHeader;			// 7zip 압축시 헤더를 암호화 할 것인가? (암호 지정시)
            int lzma2NumBlockThreads;		// lzma2 압축시 쓰레드 갯수, -1 이면 시스템 갯수만큼
            int threadPriority;				// 멀티코어를 이용해서 압축시 쓰레드 우선 순위
        };
        #endregion

        [DllImport("ArkDll.dll", EntryPoint = "SetCB_DebugCallBack", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetCB_DebugCallBack(MulticastDelegate callBack);
        public delegate void DebugCallBack([MarshalAs(UnmanagedType.LPWStr)] string txt);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Create")]
        public static extern ARKERR Create();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsCreated")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsCreated();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "TestArchive")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TestArchive();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Destroy")]
        public static extern void Destroy();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Release")]
        public static extern void Release();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CompressorRelease")]
        public static extern void CompressorRelease();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="srcLen"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Open")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Open([In] [MarshalAs(UnmanagedType.LPWStr)] string path, [In] [MarshalAs(UnmanagedType.LPWStr)] string password = null);

        /// <summary>
        /// 압축파일을 열고 목록을 읽어들입니다. 
        /// </summary>
        /// <param name="path">[in] 압축파일의 경로를 지정합니다.</param>
        /// <param name="password">[in] 압축파일의 암호를 지정합니다. 암호를 모르거나 없을 경우 NULL(0)을 전달합니다.</param>
        /// <returns>true 성공적으로 파일을 생성하였습니다. \n false 파일 생성중 문제가 발생하였습니다. 발생한 에러는 GetLastErrorArk()를 통해서 확인가능합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "OpenStream")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenStream([In] [MarshalAs(UnmanagedType.LPWStr)] string path, [In] [MarshalAs(UnmanagedType.LPWStr)] string password = null);

        /// <summary>
        /// 압축파일을 열고 목록을 읽어들입니다. 
        /// </summary>
        /// <param name="password">[in] 압축파일의 암호를 지정합니다. 암호를 모르거나 없을 경우 NULL(0)을 전달합니다.</param>
        /// <returns>true 성공적으로 파일을 생성하였습니다. \n false 파일 생성중 문제가 발생하였습니다. 발생한 에러는 GetLastErrorArk()를 통해서 확인가능합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "OpenByte")]
        [return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool OpenByte([Out] IntPtr ptr, [In] int srcLen, [In] [MarshalAs(UnmanagedType.LPWStr)] string password = null);
        public static extern bool OpenByte([Out] SafeHandle handle, [In] int srcLen, [In] [MarshalAs(UnmanagedType.LPWStr)] string password = null);

        /// <summary>
        /// 파일목록을 얻어올때 압축파일의 손상이 발견되었는지 여부를 확인합니다. 
        /// </summary>
        /// <returns></returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsBrokenArchive")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsBrokenArchive();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Close")]
        public static extern void Close();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetPassword")]
        public static extern void SetPassword([In] [MarshalAs(UnmanagedType.LPWStr)] string password);

        /// <summary>
        /// 압축파일 내의 파일 아이템 갯수를 가져옵니다. 
        /// </summary>
        /// <returns>압축파일 내의 파일 아이템 갯수를 리턴합니다. 이 함수를 호출하기 전 Open 메쏘드로 압축파일을 열어야 합니다. "파일 아이템" 목록에는 파일 뿐만 아니라 폴더도 포함됩니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetFileItemCount")]
        public static extern int GetFileItemCount();

        /// <summary>
        /// 특정 파일 아이템의 정보를 가져옵니다. 
        /// </summary>
        /// <param name="index">[in] 압축파일 아이템의 인덱스를 지정합니다. 유효한 index 파라메터의 범위는 0 부터 GetFileItemCount()-1 까지 입니다.</param>
        /// <returns>지정된 인덱스의 파일 아이템 정보를 리턴합니다. 만일 입력된 인덱스가 범위를 벗어났거나, Open메쏘드를 통해서 파일을 열지 않은 경우는 null(0)을 리턴합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetFileItem")]
        public static extern IntPtr GetFileItem([In] int index);

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetFileFormat")]
        public static extern ARK_FF GetFileFormat();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsEncryptedArchive")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsEncryptedArchive();

        /// <summary>
        /// 현재 열려있는 압축파일이 솔리드압축으로 압축된 파일인지 여부를 확인합니다. 
        /// </summary>
        /// <returns>true 압축파일에서 오류가 발견되었습니다. \n false 압축파일에서 오류가 발견되지 않았습니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsSolidArchive")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsSolidArchive();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsOpened")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsOpened();

        /// <summary>
        /// 압축파일 내의 모든 파일 아이템의 압축을 해제할 때 사용합니다.
        /// </summary>
        /// <param name="folderPath">[in] 압축을 풀 경로를 지정합니다.</param>
        /// <returns>true 모든 파일의 압축을 성공적으로 풀었습니다. \n false 압축 해제중 에러가 1건 이상 발생하였습니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ExtractAllTo")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExtractAllTo([MarshalAs(UnmanagedType.LPWStr)] string folderPath);

        /// <summary>
        /// 압축파일 내의 모든 파일 아이템의 압축을 해제할 때 사용합니다.
        /// </summary>
        /// <returns>true 모든 파일의 압축을 성공적으로 풀었습니다. \n false 압축 해제중 에러가 1건 이상 발생하였습니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ExtractAllToStream")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExtractAllToStream();

        /// <summary>
        /// 압축파일내의 한개의 파일만을 압축을 풀때 사용합니다. 
        /// </summary>
        /// <param name="index">[in] 압축을 해제할 파일의 인덱스를 지정합니다. 유효한 인덱스 파라메터는 0 부터 IArk::GetFileItemCount()-1 까지 입니다.</param>
        /// <param name="folderPath">[in] 압축을 풀 경로를 지정합니다. </param>
        /// <returns>true 성공적으로 파일을 생성하였습니다. \n false 파일 생성중 문제가 발생하였습니다. 발생한 에러는 GetLastErrorArk()를 통해서 확인가능합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ExtractOneTo")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExtractOneTo([In] int index, [In] [MarshalAs(UnmanagedType.LPWStr)] string folderPath);

        /// <summary>
      /// 압축파일내의 한개의 파일만을 압축을 풀때 사용합니다.
        /// </summary>
        /// <param name="index">[in] 압축을 해제할 파일의 인덱스를 지정합니다. 유효한 인덱스 파라메터는 0 부터 IArk::GetFileItemCount()-1 까지 입니다.</param>
        /// <returns>true 성공적으로 파일을 생성하였습니다. \n false 파일 생성중 문제가 발생하였습니다. 발생한 에러는 GetLastErrorArk()를 통해서 확인가능합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ExtractOneToStream")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ExtractOneToStream([In]int index);

        /// <summary>
      /// 압축파일내의 한개의 파일만을 압축을 풀때 사용합니다.
        /// </summary>
        /// <param name="index">[in] 압축을 해제할 파일의 인덱스를 지정합니다. 유효한 인덱스 파라메터는 0 부터 IArk::GetFileItemCount()-1 까지 입니다.</param>
        /// <param name="ptr">[out] 지정된 메모리 버퍼에 압축 데이타를 풉니다. </param>
        /// <param name="outBufLen">[in] outBuf에 할당된 버퍼 크기를 지정합니다. 할당된 크기가 압축 풀 파일의 크기보다 작을 경우 앞부분의 데이타만 풀리고 FALSE를 리턴합니다. </param>
        /// <returns>true 성공적으로 파일을 생성하였습니다. \n false 파일 생성중 문제가 발생하였습니다. 발생한 에러는 GetLastErrorArk()를 통해서 확인가능합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ExtractOneToBytes")]
        [return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool ExtractOneToBytes([In]int index, [Out] IntPtr ptr, [In] int outBufLen);
        public static extern bool ExtractOneToBytes([In]int index, [Out] SafeHandle handle, [In] int outBufLen);

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FileFormat2Str")]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static extern string FileFormat2Str();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetGlobalOpt")]
        public static extern void SetGlobalOpt();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetArchiveFileSize")]
        public static extern long GetArchiveFileSize();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetFilePathName")]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static extern string GetFilePathName();

        /// <summary>
        /// 메쏘드 호출시 에러가 발생하였을 경우 에러에 대한 상세한 에러코드값을 확인합니다. 
        /// </summary>
        /// <returns>ARKERR_NOERR : 에러가 발생하지 않았습니다</returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetLastErrorArk")]
        public static extern ARKERR GetLastErrorArk();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetLastSystemError")]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern uint GetLastSystemError();

        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CompressorInit")]
        public static extern void CompressorInit();

        /// <summary>
        /// 파일을 압축하거나 압축파일에 파일을 추가할 경우 압축 방법을 설정합니다. 
        /// </summary>
        /// <param name="opt">[in] 압축 옵션을 지정합니다. 압축포맷, 압축방법, 압축률, 암호방식, 분할압축 등에 대해서 설정할 수 있습니다. </param>
        /// <param name="password">[in] 만일 압축파일에 대해서 암호를 걸고자 할 경우 암호 문자열을 지정합니다. 압축 포맷에 따라서 char* 나 wchar_t* 값이 올 수 있습니다. </param>
        /// <param name="pwLen">[in] 암호의 길이를 바이트 단위로 지정합니다. 일반적으로 NULL 문자는 길이에 포함시키지 않습니다.</param>
        /// <returns>true 메쏘드 호출이 성공하였습니다.\n false 실패하였습니다. 파라메터에 잘못된 값이 없는지 확인해 보세요.</returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetOption")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetOption([MarshalAs(UnmanagedType.Struct)][In] ref SArkCompressorOpt opt, [In] ref byte[] password, int pwLen);

        /// <summary>
        /// 새로 압축파일을 만들거나, 기존 압축파일에 새로 파일을 추가할 경우 사용하는 메쏘드 입니다. 
        /// </summary>
        /// <param name="szSrcPathName">[in] 추가할 파일의 로컬 경로명 입니다. 이 파라메터가 NULL인 경우는 szTargetPathName 을 폴더아이템으로 처리하며, 압축파일 내에 새로 폴더를 생성할때 유용합니다. </param>
        /// <param name="szTargetPathName">[in] 압축파일에 저장될 파일의 경로명 입니다. 만일 압축파일 내에서 a 폴더 내의 b.txt 라는 이름으로 파일을 저장하고자 할 경우 L"a\\b.txt" 를 파라메터로 사용하면 됩니다. </param>
        /// <param name="owerwrite">[in] 이미 기존 내부 목록에 동일한 이름을 가진 파일이 있을 경우 기존 목록을 삭제할지 여부를 설정합니다. 내부 목록에 동일한 이름을 가지는 파일이 있고, 이 값이 FALSE 인 경우 FALSE 를 리턴합니다. </param>
        /// <param name="szFileComment">[in] 파일의 코멘트를 지정합니다. </param>
        /// <returns>true 성공적으로 파일을 생성하였습니다. \n false 파일 생성중 문제가 발생하였습니다. 발생한 에러는 GetLastErrorCompressor()를 통해서 확인가능합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "AddFileItem")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddFileItem(
            [In][MarshalAs(UnmanagedType.LPWStr)] string szSrcPathName,
            [In][MarshalAs(UnmanagedType.LPWStr)] string szTargetPathName,
            [In][MarshalAs(UnmanagedType.Bool)] bool owerwrite,
            [MarshalAs(UnmanagedType.LPWStr)] string szFileComment = null);

        /// <summary>
        /// 압축할 파일목록에 지정된 파일을 가지고 압축파일을 생성합니다. 
        /// </summary>
        /// <param name="szArchivePathName">[in] 압축파일의 경로명을 지정합니다.</param>
        /// <param name="szFileComment">[in] 압축파일에 저장될 압축파일 설명문구를 지정합니다.</param>
        /// <returns>true 성공적으로 파일을 생성하였습니다. \n false 파일 생성중 문제가 발생하였습니다. 발생한 에러는 GetLastErrorCompressor()를 통해서 확인가능합니다. </returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateArchive")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateArchive(
            [In][MarshalAs(UnmanagedType.LPWStr)] string szArchivePathName,
            [In][MarshalAs(UnmanagedType.LPWStr)] string szFileComment = null);

        /// <summary>
        /// 에러 발생시 마지막으로 발생한 에러 코드를 리턴합니다. 
        /// </summary>
        /// <returns>마지막으로 발생한 에러 코드를 리턴합니다.</returns>
        [DllImport("ArkDll.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetLastErrorCompressor")]
        public static extern ARKERR GetLastErrorCompressor();
    }

    public class ArkEvent : SingletonBase<ArkEvent>
    {
        private ArkEvent() { }

        #region OnOpening
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnOpening")]
        private static extern int SetCB_evt_OnOpening([MarshalAs(UnmanagedType.FunctionPtr)] MulticastDelegate callback);
        /// <summary>
        /// Open 메쏘드를 호출하여 파일의 목록을 읽어오고 있을때 호출됩니다. 
        /// </summary>
        /// <param name="pFileItem">[in] 목록에 추가된 파일의 정보입니다. 이 값은 압축포맷에 따라서 NULL 이 전달될 수 있습니다.</param>
        /// <param name="progress">[in] 파일의 목록을 읽어오는 진행율을 나타냅니다. 이 값은 0.0~ 100.0 사이의 값을 가지며 압축 포맷에 따라서 항상 0만 전달될 수 있습니다.</param>
        /// <param name="bStop">[out] 파일 목록을 가져오는것을 취소할지 여부를 결정할 수 있습니다. 만일 이 값을 true 로 세팅할 경우 파일 목록을 구하는 작업은 취소되고 GetLastError 결과값은 ARKERR_USER_ABORTED 가 됩니다.</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnOpening(
            [In] IntPtr pFileItem,
            [In] float progress,
            [Out] [MarshalAs(UnmanagedType.Bool)]out bool bStop);
        public OnOpening _OnOpening;
        public static void SetOnOpenning(Func<Ark.SArkFileItem, float, bool> evt) { SetCB_evt_OnOpening(evt); }
        #endregion

        #region OnStartFile
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnStartFile")]
        private static extern int SetCB_evt_OnStartFile([MarshalAs(UnmanagedType.FunctionPtr)] MulticastDelegate callback);
        /// <summary>
        /// 개별 파일아이템(파일, 폴더)의 압축을 해제할때 호출됩니다. 
        /// </summary>
        /// <param name="pFileItem">[in] 압축을 해제할 파일의 정보입니다. </param>
        /// <param name="bStopCurrent">[out] 현재 파일의 압축을 해제를 중지할지 여부를 결정할 수 있습니다.</param>
        /// <param name="bStopAll">[out] 전체 파일에 대해서 압축 해제를 중지할지 여부를 결정할 수 있습니다. </param>
        /// <param name="index"></param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnStartFile(
            [In] IntPtr pFileItem,
            [In, Out] [MarshalAs(UnmanagedType.Bool)] ref bool bStopCurrent,
            [In, Out] [MarshalAs(UnmanagedType.Bool)] ref bool bStopAll,
            [In] Int32 index);
        public static void SetOnStartFile(Func<Ark.SArkFileItem, Int32, bool> evt)
        {
            SetCB_evt_OnStartFile(evt);
        }
        #endregion

        #region OnProgressFile
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnProgressFile")]
        private static extern int SetCB_evt_OnProgressFile(MulticastDelegate callback);
        /// <summary>
        /// 파일의 압축 해제가 진행될때 호출됩니다. 
        /// </summary>
        /// <param name="pProgressInfo">[in] 압축 해제 진행율 정보입니다.</param>
        /// <param name="bStopCurrent">[out] 현재 파일의 압축을 해제를 중지할지 여부를 결정할 수 있습니다.</param>
        /// <param name="bStopAll">[out] 전체 파일에 대해서 압축 해제를 중지할지 여부를 결정할 수 있습니다.</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnProgressFile(
            //[In] [MarshalAs(UnmanagedType.Struct)] Ark.SArkProgressInfo pProgressInfo,
            [In] IntPtr pProgressInfo,
            [In, Out] [MarshalAs(UnmanagedType.Bool)] ref bool bStopCurrent,
            [In, Out] [MarshalAs(UnmanagedType.Bool)] ref bool bStopAll);
        public static void SetOnProgressFile(Func< Ark.SArkProgressInfo, bool> evt) { SetCB_evt_OnProgressFile(new OnProgressFile((IntPtr x, ref bool r1, ref bool r2) => r1 = evt((Ark.SArkProgressInfo)Marshal.PtrToStructure(x, typeof(Ark.SArkProgressInfo))))); }
        #endregion

        #region OnCompleteFile
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnCompleteFile")]
        private static extern int SetCB_evt_OnCompleteFile([MarshalAs(UnmanagedType.FunctionPtr)] MulticastDelegate callback);
        /// <summary>
        /// 개별 파일아이템의 압축 해제가 완료되었을때 호출됩니다. 
        /// </summary>
        /// <param name="pProgressInfo"><para>[in] 압축 해제 진행율 정보입니다.</para><para>자세한 사항은 <see cref="Ark.SArkProgressInfo"/> 항목을 참고하세요 </para></param>
        /// <param name="nErr">[in] 압축 해제 결과에 대한 에러코드입니다. 에러가 발생하지 않았을 경우 ARKERR_NOERR 이 전달됩니다.</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnCompleteFile(
            //[In] [MarshalAs(UnmanagedType.Struct)] Ark.SArkProgressInfo pProgressInfo, 
            [In] IntPtr pProgressInfo,
            [In] Ark.ARKERR nErr);
        public static void SetOnCompleteFile(Action<Ark.SArkProgressInfo, Ark.ARKERR> evt) { SetCB_evt_OnCompleteFile(evt); }
        #endregion

        #region OnError
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnError")]
        private static extern int SetCB_evt_OnError([MarshalAs(UnmanagedType.FunctionPtr)] MulticastDelegate callback);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnError(
            [In] Ark.ARKERR nErr,
            [In] IntPtr pFileItem,
            [In] [MarshalAs(UnmanagedType.Bool)] bool bIsWarning,
            [In, Out] [MarshalAs(UnmanagedType.Bool)] ref bool bStopAll);
        public static void SetOnError (Func<Ark.ARKERR, Ark.SArkFileItem, bool, bool> evt) { SetCB_evt_OnError(evt); }
        #endregion

        #region OnMultiVolumeFileChanged
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnMultiVolumeFileChanged")]
        private static extern int SetCB_evt_OnMultiVolumeFileChanged([MarshalAs(UnmanagedType.FunctionPtr)] MulticastDelegate callback);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnMultiVolumeFileChanged([In] [MarshalAs(UnmanagedType.LPWStr)] string szPathFileName);
        public static void SetOnMultiVolumeFileChanged(Action<string> evt)
        {
            SetCB_evt_OnMultiVolumeFileChanged(evt);
        }
        #endregion

        #region OnAskOverwrite
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnAskOverwrite")]
        private static extern int SetCB_evt_OnAskOverwrite([MarshalAs(UnmanagedType.FunctionPtr)] MulticastDelegate callback);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnAskOverwrite(
            [In] IntPtr pFileItem,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string szLocalPathName,
            [Out] out Ark.ARK_OVERWRITE_MODE overwrite,
            [Out] [MarshalAs(UnmanagedType.LPWStr)] out string pathName2Rename);
        #endregion

        #region OnAskPassword
        [DllImport("ArkDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SetCB_evt_OnAskPassword")]
        private static extern int SetCB_evt_OnAskPassword(MulticastDelegate callback);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnAskPassword(
            [In] IntPtr pFileItem,
            [In] Ark.ARK_PASSWORD_ASKTYPE askType,
            [In, Out] ref Ark.ARK_PASSWORD_RET ret,
            [Out] out string passwordW);
        #endregion

        public void Init()
        {            
            ArkEvent.SetOnOpenning((x, y) => false);
            ArkEvent.SetOnStartFile((x, y) => false);
            ArkEvent.SetOnProgressFile(x => false);
            ArkEvent.SetOnCompleteFile((x, y) => { });
            ArkEvent.SetOnError((x, y, z) => true);
            //OnMultiVolumeFileChanged
            //OnAskOverwrite
            //OnAskPassword
        }
    }
    
    public class ArkOutStream : SingletonBase<ArkOutStream>
    {
        [DllImport("ArkDll.dll", EntryPoint = "SetCB_out_Open", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetCB_out_Open(MulticastDelegate callBack);
        /// <summary>
        /// 압축 해제를 시작하기 위해서 출력 스트림을 열때 호출됩니다. 
        /// </summary>
        /// <param name="path"><para>[in] 압축을 해제할 파일명이 파라메터로 전달됩니다. 파일명은 압축파일내의 경로명을 포함합니다. </para><para>압축파일 내에 파일과 폴더가 있을 경우 파일만 콜백으로 호출되며, 폴더는 이 메쏘드를 호출하지 않습니다. </para></param>
        /// <returns><para>true : 파일을 열는데 성공하였습니다. </para><para>false : 파일을 열지 못하였습니다. 만일 FALSE 를 리턴하게 되면 ARK 라이브러리는 파일 생성이 실패한것으로 간주하고 ARKERR_CANT_OPEN_DEST_FILE 에러와 함께 현재 파일의 압축 해제를 중지하게 됩니다. </para></returns>
        public delegate bool OnOpen([MarshalAs(UnmanagedType.LPWStr)] string path);
        private OnOpen _open;
        /// <summary>
        /// 압축 해제를 시작하기 위해서 출력 스트림을 열때 호출됩니다. 
        /// <see cref="bool OnOpen(string)"/>
        /// </summary>
        public OnOpen Open { set { _open = value; SetCB_out_Open(_open); } }

        [DllImport("ArkDll.dll", EntryPoint = "SetCB_out_SetSize", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetCB_out_SetSize(MulticastDelegate callBack);
        //
        public delegate bool OnSetSize([MarshalAs(UnmanagedType.I8)] ulong size);
        private OnSetSize _setSize;
        /// <summary>
        /// 출력 스트림에 데이타를 쓰기전에 그 크기를 미리 알려주기 위해서 호출되며, 메모리 버퍼에 압축을 풀 경우에 유용합니다. 
        /// <see cref=" bool OnSetSize(ulong)"/>
        /// </summary>
        public OnSetSize SetSize { set { _setSize = value; SetCB_out_SetSize(_setSize); } }

        [DllImport("ArkDll.dll", EntryPoint = "SetCB_out_Write", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetCB_out_Write(MulticastDelegate callBack);
        public delegate bool OnWrite(IntPtr ptr, uint size);
        /// <summary>
        /// 출력 스트림에 데이타를 쓰고자 할 때 호출됩니다. 
        /// <see cref="bool Write(IntPtr ptr, uint size)"/>
        /// </summary>
        private OnWrite _write;
        public OnWrite Write { set { _write = value; SetCB_out_Write(_write); } }
        [DllImport("ArkDll.dll", EntryPoint = "SetCB_out_Close", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetCB_out_Close(MulticastDelegate callBack);
        //
        public delegate bool OnClose();
        private OnClose _close;
        /// <summary>
        /// 압축 해제가 종료되어서 파일을 핸들을 닫고자 할 때 호출됩니다. 
        /// <see cref="bool OnClose()"/>
        /// </summary>
        public OnClose Close { set { _close = value; SetCB_out_Close(_close); } }
        [DllImport("ArkDll.dll", EntryPoint = "SetCB_out_CreateFolder", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetCB_out_CreateFolder(MulticastDelegate callBack);
        //
        public delegate bool OnCreateFolder([MarshalAs(UnmanagedType.LPWStr)] string path);
        private OnCreateFolder _createFolder;
        /// <summary>
        /// 파일아이템중 폴더의 압축을 해제할때 호출됩니다. 
        /// <see cref="bool OnCreateFolder(string)"/>
        /// </summary>
        public OnCreateFolder CreateFolder { set { _createFolder = value; SetCB_out_CreateFolder(_createFolder); } }

        public ArkOutStream()
        {
            Open = x => true;
            SetSize = x => true;
            Write = (x, y) => true;
            Close = () => true;
            CreateFolder = x => true;
        }
    }
}
