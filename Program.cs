using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using NSoup;
using NSoup.Nodes;
using NSoup.Select;

namespace NaverDictDownloader
{
    internal class Program
    {

        static LinkedList<DataHolder> downloadList = new LinkedList<DataHolder>();
        
        public static void Main(string[] args)
        {
            Console.WriteLine("=== 네이버 영어사전 음성 다운로더 ===");
            bool offFlag = false;
            while (!offFlag)
            {
                Console.WriteLine($"== 다운로드할 단어 목록 : {downloadList.Count}개 ==");
                Console.WriteLine("0. 종료");
                Console.WriteLine("1. 단어 목록 추가하기");
                Console.WriteLine("2. 목록 보기");
                Console.WriteLine("3. 목록 마지막 항목 삭제");
                Console.WriteLine("4. 목록 초기화");
                Console.WriteLine("5. 다운로드 시작");
                string userInput = Console.ReadLine();
                int userChoice = 0;
                if (!Int32.TryParse(userInput, out userChoice))
                {
                    Console.WriteLine("다시 입력해 주세요");
                    continue;
                }

                switch (userChoice)
                {
                    case 0:
                        offFlag = true;
                        break;
                    case 1:
                        StartManualReader();
                        break;
                    case 2:
                        Console.WriteLine("다운로드할 목록은 다음과 같습니다");
                        foreach (DataHolder downloadData in downloadList)
                        {
                            Console.WriteLine(downloadData.WordName);
                        }
                        break;
                    case 3:
                        Console.WriteLine("마지막 대기열 항목이 삭제되었습니다");
                        if (downloadList.Count > 0) downloadList.RemoveLast();
                        break;
                    case 4:
                        Console.WriteLine("다운로드 대기열이 초기화되었습니다");
                        downloadList.Clear();
                        break;
                    case 5:
                        StartDownload();
                        break;
                    default:
                        Console.WriteLine("다시 입력해 주세요");
                        break;
                }
            }
        }

        private static void StartManualReader()
        {
            Console.WriteLine("단어를 입력해주세요");
            string userInput = Console.ReadLine();
            if (userInput == null || userInput.Equals(String.Empty)) return;
            DataHolder downloadData = new DataHolder();
            downloadData.WordName = userInput;
            downloadData.FileName = string.Format("{0:000}", downloadList.Count + 1) + "_" + userInput + ".mp3";
            //downloadData.FileName = "초등6_" +string.Format("{0:000}", downloadList.Count + 1) + ".mp3";
            downloadList.AddLast(downloadData);
        }
        
        private static void StartDownload()
        {
            Console.WriteLine("다운로드를 시작합니다");

            string dirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\download";
            System.IO.Directory.CreateDirectory(dirPath);

            WebClient downloadClient = new WebClient {Encoding = Encoding.UTF8};

            foreach (DataHolder downloadData in downloadList)
            {
                string wordWebFile;
                if (!GetWordWebFile(downloadData.WordName, out wordWebFile, out downloadData.IsSecondary))
                {
                    downloadData.IsSuccess = false;
                    continue;
                }
                Console.WriteLine($"{downloadData.WordName} : {wordWebFile}");
                downloadClient.DownloadFile(wordWebFile, dirPath + "\\" + downloadData.FileName);
                downloadData.IsSuccess = true;
            }
            
            Console.WriteLine("다운로드가 완료되었습니다");
            
            foreach (DataHolder downloadData in downloadList)
            {
                if (!downloadData.IsSuccess) Console.WriteLine("다운로드 실패 : " + downloadData.WordName);
                else if (downloadData.IsSecondary) Console.WriteLine("대안 음성 다운로드됨 : " + downloadData.WordName);
            }
            
        }

        private static bool GetWordWebFile(string wordName, out string outWordWebURL, out bool isSecondary)
        {
            try
            {
                string requestURL = $"https://endic.naver.com/search.nhn?searchOption=all&query={wordName}&forceRedirect=N&isOnlyViewEE=N&sLn=kr&oldUser";
                string webResult = "";
                
                //WebClient crawlerClient = new WebClient {Encoding = Encoding.UTF8};
                //webResult = crawlerClient.DownloadString(requestURL);

                //webResult = GetRequest(requestURL);
                
                Document originDoc = NSoupClient.Parse(new Uri(requestURL), 5000);
                Element searchElement = originDoc.Select(".word_num").First;
                Elements originWords = searchElement.Select("dt");

                outWordWebURL = "";
                isSecondary = false;
                
                foreach (Element originWord in originWords)
                {
                    string targetWord = originWord.Select("strong").Text;
                    if (wordName.ToLower().Equals(targetWord.ToLower()))
                    {
                        Elements listenItems = originWord.Select("._soundPlay");
                        foreach (Element listenItem in listenItems)
                        {
                            if (listenItem.HasAttr("playlist"))
                            {
                                outWordWebURL = listenItem.Attr("playlist");
                                return true;
                            }
                            isSecondary = true;
                        }
                    }
                }
                
                return false;
            }
            catch (Exception e)
            {
                outWordWebURL = "";
                isSecondary = false;
                return false;
            }
        }
    }
}