using FFXIVTool.Models;
using FFXIVTool.Utility;
using FFXIVTool.ViewModel;
using FFXIVTool.Views;
using FFXIVTool.Windows;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using SaintCoinach;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WepTuple = System.Tuple<int, int, int, int>;

namespace FFXIVTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{

		public int Processcheck = 0;
		public static bool HasRead = false;
		public static ARealmReversed Realm;
		public static bool CurrentlySaving = false;
		readonly string exepath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
		public CharacterDetails CharacterDetails { get => (CharacterDetails)BaseViewModel.model; set => BaseViewModel.model = value; }

		readonly Version version = Assembly.GetExecutingAssembly().GetName().Version;
		public MainWindow()
		{
			ServicePointManager.SecurityProtocol = (ServicePointManager.SecurityProtocol & SecurityProtocolType.Ssl3) | (SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);

			// Call the update method.
			UpdateProgram();

			if (!File.Exists(@"./OffsetSettings.xml"))
			{
				try
				{
					string xmlStr;
					using (var wc = new WebClient())
					{
						xmlStr = wc.DownloadString(@"https://raw.githubusercontent.com/imchillin/SSTool/master/FFXIVTool/OffsetSettings.xml");
					}
					var xmlDoc = new System.Xml.XmlDocument();
					xmlDoc.LoadXml(xmlStr);
					File.WriteAllText(@"./OffsetSettings.xml", xmlDoc.InnerXml);
				}
				catch
				{
					MessageBox.Show("Unable to connect to the remote server - No connection could be made because the target machine actively refused it! \n If you wish to pursue using this application please download an updated OffsetSettings via discord.", "Oh no!");
					Close();
					return;
				}
			}
			List<ProcessLooker.Game> GameList = new List<ProcessLooker.Game>();
			Process[] processlist = Process.GetProcesses();
			Processcheck = 0;
			foreach (Process theprocess in processlist)
			{
				if (theprocess.ProcessName.ToLower().Contains("ffxiv_dx11"))
				{
					Processcheck++;
					GameList.Add(new ProcessLooker.Game() { ProcessName = theprocess.ProcessName, ID = theprocess.Id, StartTime = theprocess.StartTime, AppIcon = IconToImageSource(System.Drawing.Icon.ExtractAssociatedIcon(theprocess.MainModule.FileName)) });
				}
			}
			if (Processcheck > 1)
			{
				ProcessLooker f = new ProcessLooker(GameList);
				f.ShowDialog();
				if (f.Choice == null)
				{
					Close();
					return;
				}
				MainViewModel.gameProcId = f.Choice.ID;
			}
			if (Processcheck == 1)
				MainViewModel.gameProcId = GameList[0].ID;
			if (Processcheck <= 0)
			{
				ProcessLooker f = new ProcessLooker(GameList);
				f.ShowDialog();
				if (f.Choice == null)
				{
					Close();
					return;
				}
				MainViewModel.gameProcId = f.Choice.ID;
			}
			InitializeComponent();
		}

		private void UpdateProgram()
		{
			ServicePointManager.SecurityProtocol = (ServicePointManager.SecurityProtocol & SecurityProtocolType.Ssl3) | (SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);

			// Delete the old updater file.
			if (File.Exists(".SSTU.old"))
				File.Delete(".SSTU.old");
			try
			{
				var proc = new Process();
				proc.StartInfo.FileName = Path.Combine(Environment.CurrentDirectory, "SSToolUpdater.exe");
				proc.StartInfo.UseShellExecute = true;
				proc.StartInfo.Verb = "runas";
				proc.Start();
				proc.WaitForExit();
				proc.Dispose();
			}
			catch (Exception)
			{
				var result = MessageBox.Show(
					"Couldn't run the updater. Would you like to visit the releases page to check for a new update manually?",
					"SSTool",
					MessageBoxButton.YesNo,
					MessageBoxImage.Error
				);

				// Launch the web browser to the latest release.
				if (result == MessageBoxResult.Yes)
				{
					Process.Start("https://github.com/imchillin/SSTool/releases/latest");
				}
			}
		}

		public bool AdminNeeded()
		{
			try
			{
				File.WriteAllText(exepath + "\\test.txt", "test");
				File.Delete(exepath + "\\test.txt");
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return true;
			}
		}
		public static ImageSource IconToImageSource(System.Drawing.Icon icon)
		{
			return Imaging.CreateBitmapSourceFromHIcon(
				icon.Handle,
				new Int32Rect(0, 0, icon.Width, icon.Height),
				BitmapSizeOptions.FromEmptyOptions());
		}
		private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Title = $"SSTool v{version} By: LeonBlade, Johto and Krisan Thyme";
			DataContext = new MainViewModel();
			var settings = SaveSettings.Default;
			var accentColor = settings.Accent;
			new PaletteHelper().ReplaceAccentColor(accentColor);
			var primaryColor = settings.Primary;
			new PaletteHelper().ReplacePrimaryColor(primaryColor);
			var theme = settings.Theme;
			new PaletteHelper().SetLightDark(theme != "Light");
			Topmost = settings.TopApp;
			// toggle status
			(DataContext as MainViewModel).ToggleStatus(settings.TopApp);
			if (settings.ReminderTool == false)
			{
				var msgResult = MessageBox.Show("This is reminder to anyone who may not know that we have a discord or isn't in our discord to know that we have one for reports/support/help and general discussion! If you wish to join click Yes, otherwise click No.", "Reminder!", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
				if (msgResult == MessageBoxResult.Yes)
				{

					Process.Start("https://discord.gg/hq3DnBa");
					SaveSettings.Default.ReminderTool = true;
				}
				else
				{
					SaveSettings.Default.ReminderTool = true;
				}
			}
			CharacterDetailsView._exdProvider.MakeCharaMakeFeatureList();
			CharacterDetailsView._exdProvider.MakeCharaMakeFeatureFacialList();
			CharacterDetailsView._exdProvider.MakeTerritoryTypeList();
		}

		private void CharacterRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			var m = MemoryManager.Instance.MemLib;
			var c = Settings.Instance.Character;

			string GAS(params string[] args) => MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, args);
			var xdad = (byte)MemoryManager.Instance.MemLib.readByte(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.EntityType));
			if (xdad == 1)
			{
				m.writeMemory(GAS(c.EntityType), "byte", "2");
				m.writeMemory(GAS(c.RenderToggle), "int", "2");
				Task.Delay(50).Wait();
				m.writeMemory(GAS(c.RenderToggle), "int", "0");
				Task.Delay(50).Wait();
				m.writeMemory(GAS(c.EntityType), "byte", "1");
			}
			else
			{
				m.writeMemory(GAS(c.RenderToggle), "int", "2");
				Task.Delay(50).Wait();
				m.writeMemory(GAS(c.RenderToggle), "int", "0");
			}
		}

		private void FindProcess_Click(object sender, RoutedEventArgs e)
		{
			List<ProcessLooker.Game> GameList = new List<ProcessLooker.Game>();

			Process[] processlist = Process.GetProcesses();
			Processcheck = 0;
			foreach (Process theprocess in processlist)
			{
				if (theprocess.ProcessName.ToLower().Contains("ffxiv_dx11"))
				{
					Processcheck++;
					GameList.Add(new ProcessLooker.Game() { ProcessName = theprocess.ProcessName, ID = theprocess.Id, StartTime = theprocess.StartTime, AppIcon = IconToImageSource(System.Drawing.Icon.ExtractAssociatedIcon(theprocess.MainModule.FileName)) });
				}
			}
			if (Processcheck > 1)
			{
				ProcessLooker f = new ProcessLooker(GameList);
				f.ShowDialog();
				if (f.Choice == null)
					return;
				MainViewModel.ShutDownStuff();
				MainViewModel.gameProcId = f.Choice.ID;
				DataContext = new MainViewModel();
			}
			if (Processcheck == 1)
			{
				MainViewModel.ShutDownStuff();
				MainViewModel.gameProcId = GameList[0].ID;
				DataContext = new MainViewModel();
			}
		}

		private void TwitterButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://twitter.com/ffxivsstool");
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			CurrentlySaving = true;
			if (SaveSettings.Default.WindowsExplorer)
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "SSTool", "Saves");
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
				var d = new SaveFileDialog
				{
					Filter = "Json File(*.json)|*.json",
					InitialDirectory = path
				};
				if (d.ShowDialog() == true)
				{
					var Save1 = new CharSaves(); // Gearsave is class with all 
					var extension = Path.GetExtension(".json");
					var result = d.SafeFileName.Substring(0, d.SafeFileName.Length - extension.Length);
					Save1.Description = result;
					Save1.DateCreated = DateTime.Now.ToLocalTime().ToString();
					Save1.MainHand = new WepTuple(CharacterDetails.Job.value, CharacterDetails.WeaponBase.value, CharacterDetails.WeaponV.value, CharacterDetails.WeaponDye.value);
					Save1.OffHand = new WepTuple(CharacterDetails.Offhand.value, CharacterDetails.OffhandBase.value, CharacterDetails.OffhandV.value, CharacterDetails.OffhandDye.value);
					Save1.EquipmentBytes = CharacterDetails.TestArray2.value;
					Save1.CharacterBytes = CharacterDetails.TestArray.value;
					Save1.CharacterDetails = CharacterDetails;
					var details = JsonConvert.SerializeObject(Save1, Formatting.Indented);
					File.WriteAllText(d.FileName, details);
					CurrentlySaving = false;
				}
				else CurrentlySaving = false;
			}
			else
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "SSTool", "Saves");
				if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
				var c = new GearSave("Save Character Save", "Write Character Save name here...")
				{
					Owner = Application.Current.MainWindow
				};
				c.ShowDialog();
				if (c.Filename == null) 
				{ 
					CurrentlySaving = false; 
					return; 
				}
				else
				{
					var Save1 = new CharSaves
					{
						Description = c.Filename,
						DateCreated = DateTime.Now.ToLocalTime().ToString(),
						MainHand = new WepTuple(CharacterDetails.Job.value, CharacterDetails.WeaponBase.value, CharacterDetails.WeaponV.value, CharacterDetails.WeaponDye.value),
						OffHand = new WepTuple(CharacterDetails.Offhand.value, CharacterDetails.OffhandBase.value, CharacterDetails.OffhandV.value, CharacterDetails.OffhandDye.value),
						EquipmentBytes = CharacterDetails.TestArray2.value,
						CharacterBytes = CharacterDetails.TestArray.value,
						CharacterDetails = CharacterDetails
					}; 
					// Gearsave is class with all address
					string details = JsonConvert.SerializeObject(Save1, Formatting.Indented);
					File.WriteAllText(Path.Combine(path, c.Filename + ".json"), details);
					CurrentlySaving = false;
				}
			}
		}
		private void Load_Click(object sender, RoutedEventArgs e)
		{
			var c = new LoadWindow
			{
				Owner = this
			};
			c.ShowDialog();

			if (c.Choice == "All") LoadActor_All();
			else if (c.Choice == "App") LoadActor_Appearance();
			else if (c.Choice == "Xuip") LoadActor_Equipment();
			else if (c.Choice == "Dat") LoadActor_Dat();
			else if (c.Choice == "Gearset") LoadActor_Gearset();
		}
		private void LoadActor_Gearset()
		{
			if (!SaveSettings.Default.WindowsExplorer)
			{
				var w = new GearsetChooseWindow("Select the saved gearset you want to load.")
				{
					Owner = Application.Current.MainWindow
				};
				w.ShowDialog();
				if (w.Choice != null)
				{
					LoadGearset(w.Choice);
				}
				else return;
			}
			else
			{
				var dig = new OpenFileDialog();
				var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "SSTool", "Gearsets");
				if (!Directory.Exists(path)) Directory.CreateDirectory(path);
				dig.InitialDirectory = path;
				dig.Filter = "Json File(*.json)|*.json";
				dig.DefaultExt = ".json";
				if (dig.ShowDialog() == true)
				{
					var load1 = JsonConvert.DeserializeObject<GearSaves>(File.ReadAllText(dig.FileName));
					LoadGearset(load1);
				}
				else return;
			}
		}
		private void LoadGearset(GearSaves equpmentarray)
		{
			try
			{
				Load.IsEnabled = false;
				byte[] EquipmentArray;
				EquipmentArray = MemoryManager.StringToByteArray(equpmentarray.EquipmentBytes.Replace(" ", string.Empty));
				if (EquipmentArray == null) return;
				CharacterDetails.Offhand.freeze = true;
				CharacterDetails.Job.freeze = true;
				CharacterDetails.HeadPiece.freeze = true;
				CharacterDetails.Chest.freeze = true;
				CharacterDetails.Arms.freeze = true;
				CharacterDetails.Legs.freeze = true;
				CharacterDetails.Feet.freeze = true;
				CharacterDetails.Ear.freeze = true;
				CharacterDetails.Neck.freeze = true;
				CharacterDetails.Wrist.freeze = true;
				CharacterDetails.RFinger.freeze = true;
				CharacterDetails.LFinger.freeze = true;
				Task.Delay(25).Wait();
				CharacterDetails.HeadPiece.value = (EquipmentArray[0] + EquipmentArray[1] * 256);
				CharacterDetails.HeadV.value = EquipmentArray[2];
				CharacterDetails.HeadDye.value = EquipmentArray[3];
				CharacterDetails.Chest.value = (EquipmentArray[4] + EquipmentArray[5] * 256);
				CharacterDetails.ChestV.value = EquipmentArray[6];
				CharacterDetails.ChestDye.value = EquipmentArray[7];
				CharacterDetails.Arms.value = (EquipmentArray[8] + EquipmentArray[9] * 256);
				CharacterDetails.ArmsV.value = EquipmentArray[10];
				CharacterDetails.ArmsDye.value = EquipmentArray[11];
				CharacterDetails.Legs.value = (EquipmentArray[12] + EquipmentArray[13] * 256);
				CharacterDetails.LegsV.value = EquipmentArray[14];
				CharacterDetails.LegsDye.value = EquipmentArray[15];
				CharacterDetails.Feet.value = (EquipmentArray[16] + EquipmentArray[17] * 256);
				CharacterDetails.FeetVa.value = EquipmentArray[18];
				CharacterDetails.FeetDye.value = EquipmentArray[19];
				CharacterDetails.Ear.value = (EquipmentArray[20] + EquipmentArray[21] * 256);
				CharacterDetails.EarVa.value = EquipmentArray[22];
				CharacterDetails.Neck.value = (EquipmentArray[24] + EquipmentArray[25] * 256);
				CharacterDetails.NeckVa.value = EquipmentArray[26];
				CharacterDetails.Wrist.value = (EquipmentArray[28] + EquipmentArray[29] * 256);
				CharacterDetails.WristVa.value = EquipmentArray[30];
				CharacterDetails.RFinger.value = (EquipmentArray[32] + EquipmentArray[33] * 256);
				CharacterDetails.RFingerVa.value = EquipmentArray[34];
				CharacterDetails.LFinger.value = (EquipmentArray[36] + EquipmentArray[37] * 256);
				CharacterDetails.LFingerVa.value = EquipmentArray[38];
				MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HeadPiece), EquipmentArray);
				CharacterDetails.Job.value = equpmentarray.MainHand.Item1;
				CharacterDetails.WeaponBase.value = (byte)equpmentarray.MainHand.Item2;
				CharacterDetails.WeaponV.value = (byte)equpmentarray.MainHand.Item3;
				CharacterDetails.WeaponDye.value = (byte)equpmentarray.MainHand.Item4;
				MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Job), EquipmentFlyOut.WepTupleToByteAry(equpmentarray.MainHand));
				CharacterDetails.Offhand.value = equpmentarray.OffHand.Item1;
				CharacterDetails.OffhandBase.value = (byte)equpmentarray.OffHand.Item2;
				CharacterDetails.OffhandV.value = (byte)equpmentarray.OffHand.Item3;
				CharacterDetails.OffhandDye.value = (byte)equpmentarray.OffHand.Item4;
				MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Offhand), EquipmentFlyOut.WepTupleToByteAry(equpmentarray.OffHand));
				Load.IsEnabled = true;
			}
			catch (Exception exc)
			{
				MessageBox.Show("One or more fields were not formatted correctly.\n\n" + exc, " Error " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version, MessageBoxButton.OK, MessageBoxImage.Error);
				Load.IsEnabled = true;
			}
		}
		private void LoadActor_Dat()
		{
			var w = new CharacterSaveChooseWindow()
			{
				Owner = this
			};
			w.ShowDialog();
			if (w.Choice != null)
			{
				Load.IsEnabled = false;
				CharacterDetails.Race.freeze = true;
				CharacterDetails.Clan.freeze = true;
				CharacterDetails.Gender.freeze = true;
				CharacterDetails.Head.freeze = true;
				CharacterDetails.TailType.freeze = true;
				CharacterDetails.LimbalEyes.freeze = true;
				CharacterDetails.Nose.freeze = true;
				CharacterDetails.Lips.freeze = true;
				CharacterDetails.BodyType.freeze = true;
				CharacterDetails.Highlights.freeze = true;
				CharacterDetails.Hair.freeze = true;
				CharacterDetails.HairTone.freeze = true;
				CharacterDetails.HighlightTone.freeze = true;
				CharacterDetails.Jaw.freeze = true;
				CharacterDetails.RBust.freeze = true;
				CharacterDetails.RHeight.freeze = true;
				CharacterDetails.LipsTone.freeze = true;
				CharacterDetails.Skintone.freeze = true;
				CharacterDetails.FacialFeatures.freeze = true;
				CharacterDetails.TailorMuscle.freeze = true;
				CharacterDetails.Eye.freeze = true;
				CharacterDetails.RightEye.freeze = true;
				CharacterDetails.EyeBrowType.freeze = true;
				CharacterDetails.LeftEye.freeze = true;
				CharacterDetails.FacePaint.freeze = true;
				CharacterDetails.FacePaintColor.freeze = true;
				Task.Delay(25).Wait();
				CharacterDetails.Race.value = w.Choice[0];
				CharacterDetails.Gender.value = w.Choice[1];
				CharacterDetails.BodyType.value = w.Choice[2];
				CharacterDetails.RHeight.value = w.Choice[3];
				CharacterDetails.Clan.value = w.Choice[4];
				CharacterDetails.Head.value = w.Choice[5];
				CharacterDetails.Hair.value = w.Choice[6];
				CharacterDetails.Highlights.value = w.Choice[7];
				CharacterDetails.Skintone.value = w.Choice[8];
				CharacterDetails.RightEye.value = w.Choice[9];
				CharacterDetails.HairTone.value = w.Choice[10];
				CharacterDetails.HighlightTone.value = w.Choice[11];
				CharacterDetails.FacialFeatures.value = w.Choice[12];
				CharacterDetails.LimbalEyes.value = w.Choice[13];
				CharacterDetails.EyeBrowType.value = w.Choice[14];
				CharacterDetails.LeftEye.value = w.Choice[15];
				CharacterDetails.Eye.value = w.Choice[16];
				CharacterDetails.Nose.value = w.Choice[17];
				CharacterDetails.Jaw.value = w.Choice[18];
				CharacterDetails.Lips.value = w.Choice[19];
				CharacterDetails.LipsTone.value = w.Choice[20];
				CharacterDetails.TailorMuscle.value = w.Choice[21];
				CharacterDetails.TailType.value = w.Choice[22];
				CharacterDetails.RBust.value = w.Choice[23];
				CharacterDetails.FacePaint.value = w.Choice[24];
				CharacterDetails.FacePaintColor.value = w.Choice[25];
				MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Race), w.Choice);
				Load.IsEnabled = true;
			}
		}
		private void LoadActor_All()
		{
			if (!SaveSettings.Default.WindowsExplorer)
			{
				var w = new SaveChooseWindow("Select saved Character[All].")
				{
					Owner = Application.Current.MainWindow
				};
				w.ShowDialog();
				if (w.Choice != null)
				{
					LoadActor(w.Choice, 0);
				}
				else return;
			}
			else
			{
				OpenFileDialog dig = new OpenFileDialog
				{
					InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "SSTool", "Saves"),
					Filter = "Json File(*.json)|*.json",
					DefaultExt = ".json"
				};
				if (dig.ShowDialog() == true)
				{
					CharSaves load1 = JsonConvert.DeserializeObject<CharSaves>(File.ReadAllText(dig.FileName));
					LoadActor(load1, 0);
				}
				else return;
			}
		}
		private void LoadActor_Appearance()
		{
			if (!SaveSettings.Default.WindowsExplorer)
			{
				var w = new SaveChooseWindow("Select saved Character[Appearance].")
				{
					Owner = Application.Current.MainWindow
				};
				w.ShowDialog();
				if (w.Choice != null)
				{
					LoadActor(w.Choice, 1);
				}
				else return;
			}
			else
			{
				var d = new OpenFileDialog
				{
					InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "SSTool", "Saves"),
					Filter = "Json File(*.json)|*.json",
					DefaultExt = ".json"
				};
				if (d.ShowDialog() == true)
				{
					CharSaves load1 = JsonConvert.DeserializeObject<CharSaves>(File.ReadAllText(d.FileName));
					LoadActor(load1, 1);
				}
				else return;
			}
		}
		private void LoadActor_Equipment()
		{
			if (!SaveSettings.Default.WindowsExplorer)
			{
				var w = new SaveChooseWindow("Select the Character[Equipment].")
				{
					Owner = Application.Current.MainWindow
				};
				w.ShowDialog();
				if (w.Choice != null)
				{
					LoadActor(w.Choice, 2);
				}
				else return;
			}
			else
			{
				var d = new OpenFileDialog
				{
					InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), "SSTool", "Saves"),
					Filter = "Json File(*.json)|*.json",
					DefaultExt = ".json"
				};
				if (d.ShowDialog() == true)
				{
					CharSaves load1 = JsonConvert.DeserializeObject<CharSaves>(File.ReadAllText(d.FileName));
					LoadActor(load1, 2);
				}
				else return;
			}
		}
		private void LoadActor(CharSaves charSaves, int savechoice)
		{
			try
			{
				Load.IsEnabled = false;
				if (savechoice == 0 || savechoice == 1)
				{
					CharacterDetails.Race.freeze = true;
					CharacterDetails.Clan.freeze = true;
					CharacterDetails.Gender.freeze = true;
					CharacterDetails.Head.freeze = true;
					CharacterDetails.TailType.freeze = true;
					CharacterDetails.LimbalEyes.freeze = true;
					CharacterDetails.Nose.freeze = true;
					CharacterDetails.Lips.freeze = true;
					CharacterDetails.BodyType.freeze = true;
					CharacterDetails.Highlights.freeze = true;
					CharacterDetails.Voices.freeze = true;
					CharacterDetails.Hair.freeze = true;
					CharacterDetails.HairTone.freeze = true;
					CharacterDetails.HighlightTone.freeze = true;
					CharacterDetails.Jaw.freeze = true;
					CharacterDetails.RBust.freeze = true;
					CharacterDetails.RHeight.freeze = true;
					CharacterDetails.LipsTone.freeze = true;
					CharacterDetails.Skintone.freeze = true;
					CharacterDetails.FacialFeatures.freeze = true;
					CharacterDetails.TailorMuscle.freeze = true;
					CharacterDetails.Eye.freeze = true;
					CharacterDetails.RightEye.freeze = true;
					CharacterDetails.EyeBrowType.freeze = true;
					CharacterDetails.LeftEye.freeze = true;
					CharacterDetails.FacePaint.freeze = true;
					CharacterDetails.FacePaintColor.freeze = true;
					if (CharacterDetails.RightEyeBlue.freeze == true) { CharacterDetails.RightEyeBlue.freeze = false; CharacterDetails.RightEyeBlue.freezetest = true; }
					if (CharacterDetails.RightEyeGreen.freeze == true) { CharacterDetails.RightEyeGreen.freeze = false; CharacterDetails.RightEyeGreen.freezetest = true; }
					if (CharacterDetails.RightEyeRed.freeze == true) { CharacterDetails.RightEyeRed.freeze = false; CharacterDetails.RightEyeRed.freezetest = true; }
					if (CharacterDetails.LeftEyeBlue.freeze == true) { CharacterDetails.LeftEyeBlue.freeze = false; CharacterDetails.LeftEyeBlue.freezetest = true; }
					if (CharacterDetails.LeftEyeGreen.freeze == true) { CharacterDetails.LeftEyeGreen.freeze = false; CharacterDetails.LeftEyeGreen.freezetest = true; }
					if (CharacterDetails.LeftEyeRed.freeze == true) { CharacterDetails.LeftEyeRed.freeze = false; CharacterDetails.LeftEyeRed.freezetest = true; }
					if (CharacterDetails.LipsB.freeze == true) { CharacterDetails.LipsB.freeze = false; CharacterDetails.LipsB.freezetest = true; }
					if (CharacterDetails.LipsG.freeze == true) { CharacterDetails.LipsG.freeze = false; CharacterDetails.LipsG.freezetest = true; }
					if (CharacterDetails.LipsR.freeze == true) { CharacterDetails.LipsR.freeze = false; CharacterDetails.LipsR.freezetest = true; }
					if (CharacterDetails.LimbalB.freeze == true) { CharacterDetails.LimbalB.freeze = false; CharacterDetails.LimbalB.freezetest = true; }
					if (CharacterDetails.LimbalG.freeze == true) { CharacterDetails.LimbalG.freeze = false; CharacterDetails.LimbalG.freezetest = true; }
					if (CharacterDetails.LimbalR.freeze == true) { CharacterDetails.LimbalR.freeze = false; CharacterDetails.LimbalR.freezetest = true; }
					if (CharacterDetails.MuscleTone.freeze == true) { CharacterDetails.MuscleTone.freeze = false; CharacterDetails.MuscleTone.freezetest = true; }
					if (CharacterDetails.TailSize.freeze == true) { CharacterDetails.TailSize.freeze = false; CharacterDetails.TailSize.freezetest = true; }
					if (CharacterDetails.BustX.freeze == true) { CharacterDetails.BustX.freeze = false; CharacterDetails.BustX.freezetest = true; }
					if (CharacterDetails.BustY.freeze == true) { CharacterDetails.BustY.freeze = false; CharacterDetails.BustY.freezetest = true; }
					if (CharacterDetails.BustZ.freeze == true) { CharacterDetails.BustZ.freeze = false; CharacterDetails.BustZ.freezetest = true; }
					if (CharacterDetails.LipsBrightness.freeze == true) { CharacterDetails.LipsBrightness.freeze = false; CharacterDetails.LipsBrightness.freezetest = true; }
					if (CharacterDetails.SkinBlueGloss.freeze == true) { CharacterDetails.SkinBlueGloss.freeze = false; CharacterDetails.SkinBlueGloss.freezetest = true; }
					if (CharacterDetails.SkinGreenGloss.freeze == true) { CharacterDetails.SkinGreenGloss.freeze = false; CharacterDetails.SkinGreenGloss.freezetest = true; }
					if (CharacterDetails.SkinRedGloss.freeze == true) { CharacterDetails.SkinRedGloss.freeze = false; CharacterDetails.SkinRedGloss.freezetest = true; }
					if (CharacterDetails.SkinBluePigment.freeze == true) { CharacterDetails.SkinBluePigment.freeze = false; CharacterDetails.SkinBluePigment.freezetest = true; }
					if (CharacterDetails.SkinGreenPigment.freeze == true) { CharacterDetails.SkinGreenPigment.freeze = false; CharacterDetails.SkinGreenPigment.freezetest = true; }
					if (CharacterDetails.SkinRedPigment.freeze == true) { CharacterDetails.SkinRedPigment.freeze = false; CharacterDetails.SkinRedPigment.freezetest = true; }
					if (CharacterDetails.HighlightBluePigment.freeze == true) { CharacterDetails.HighlightBluePigment.freeze = false; CharacterDetails.HighlightBluePigment.freezetest = true; }
					if (CharacterDetails.HighlightGreenPigment.freeze == true) { CharacterDetails.HighlightGreenPigment.freeze = false; CharacterDetails.HighlightGreenPigment.freezetest = true; }
					if (CharacterDetails.HighlightRedPigment.freeze == true) { CharacterDetails.HighlightRedPigment.freeze = false; CharacterDetails.HighlightRedPigment.freezetest = true; }
					if (CharacterDetails.HairGlowBlue.freeze == true) { CharacterDetails.HairGlowBlue.freeze = false; CharacterDetails.HairGlowBlue.freezetest = true; }
					if (CharacterDetails.HairGlowGreen.freeze == true) { CharacterDetails.HairGlowGreen.freeze = false; CharacterDetails.HairGlowGreen.freezetest = true; }
					if (CharacterDetails.HairGlowRed.freeze == true) { CharacterDetails.HairGlowRed.freeze = false; CharacterDetails.HairGlowRed.freezetest = true; }
					if (CharacterDetails.HairGreenPigment.freeze == true) { CharacterDetails.HairGreenPigment.freeze = false; CharacterDetails.HairGreenPigment.freezetest = true; }
					if (CharacterDetails.HairBluePigment.freeze == true) { CharacterDetails.HairBluePigment.freeze = false; CharacterDetails.HairBluePigment.freezetest = true; }
					if (CharacterDetails.HairRedPigment.freeze == true) { CharacterDetails.HairRedPigment.freeze = false; CharacterDetails.HairRedPigment.freezetest = true; }
					if (CharacterDetails.Height.freeze == true) { CharacterDetails.Height.freeze = false; CharacterDetails.Height.freezetest = true; }
				} // 0 = All ; 1= Appearance; 2=Equipment
				if (savechoice == 0 || savechoice == 2)
				{
					CharacterDetails.Offhand.freeze = true;
					CharacterDetails.Job.freeze = true;
					CharacterDetails.HeadPiece.freeze = true;
					CharacterDetails.Chest.freeze = true;
					CharacterDetails.Arms.freeze = true;
					CharacterDetails.Legs.freeze = true;
					CharacterDetails.Feet.freeze = true;
					CharacterDetails.Ear.freeze = true;
					CharacterDetails.Neck.freeze = true;
					CharacterDetails.Wrist.freeze = true;
					CharacterDetails.RFinger.freeze = true;
					CharacterDetails.LFinger.freeze = true;
					if (CharacterDetails.WeaponGreen.freeze == true) { CharacterDetails.WeaponGreen.freeze = false; CharacterDetails.WeaponGreen.Cantbeused = true; }
					if (CharacterDetails.WeaponBlue.freeze == true) { CharacterDetails.WeaponBlue.freeze = false; CharacterDetails.WeaponBlue.Cantbeused = true; }
					if (CharacterDetails.WeaponRed.freeze == true) { CharacterDetails.WeaponRed.freeze = false; CharacterDetails.WeaponRed.Cantbeused = true; }
					if (CharacterDetails.WeaponZ.freeze == true) { CharacterDetails.WeaponZ.freeze = false; CharacterDetails.WeaponZ.Cantbeused = true; }
					if (CharacterDetails.WeaponY.freeze == true) { CharacterDetails.WeaponY.freeze = false; CharacterDetails.WeaponY.Cantbeused = true; }
					if (CharacterDetails.WeaponX.freeze == true) { CharacterDetails.WeaponX.freeze = false; CharacterDetails.WeaponX.Cantbeused = true; }
					if (CharacterDetails.OffhandZ.freeze == true) { CharacterDetails.OffhandZ.freeze = false; CharacterDetails.OffhandZ.Cantbeused = true; }
					if (CharacterDetails.OffhandY.freeze == true) { CharacterDetails.OffhandY.freeze = false; CharacterDetails.OffhandY.Cantbeused = true; }
					if (CharacterDetails.OffhandX.freeze == true) { CharacterDetails.OffhandX.freeze = false; CharacterDetails.OffhandX.Cantbeused = true; }
					if (CharacterDetails.OffhandRed.freeze == true) { CharacterDetails.OffhandRed.freeze = false; CharacterDetails.OffhandRed.Cantbeused = true; }
					if (CharacterDetails.OffhandBlue.freeze == true) { CharacterDetails.OffhandBlue.freeze = false; CharacterDetails.OffhandBlue.Cantbeused = true; }
					if (CharacterDetails.OffhandGreen.freeze == true) { CharacterDetails.OffhandGreen.freeze = false; CharacterDetails.OffhandGreen.Cantbeused = true; }
				}
				Task.Delay(45).Wait();
				{
					if (savechoice == 0 || savechoice == 1)
					{
						byte[] CharacterBytes;
						CharacterBytes = MemoryManager.StringToByteArray(charSaves.CharacterBytes.Replace(" ", string.Empty));
						CharacterDetails.Race.value = CharacterBytes[0];
						CharacterDetails.Gender.value = CharacterBytes[1];
						CharacterDetails.BodyType.value = CharacterBytes[2];
						CharacterDetails.RHeight.value = CharacterBytes[3];
						CharacterDetails.Clan.value = CharacterBytes[4];
						CharacterDetails.Head.value = CharacterBytes[5];
						CharacterDetails.Hair.value = CharacterBytes[6];
						CharacterDetails.Highlights.value = CharacterBytes[7];
						CharacterDetails.Skintone.value = CharacterBytes[8];
						CharacterDetails.RightEye.value = CharacterBytes[9];
						CharacterDetails.HairTone.value = CharacterBytes[10];
						CharacterDetails.HighlightTone.value = CharacterBytes[11];
						CharacterDetails.FacialFeatures.value = CharacterBytes[12];
						CharacterDetails.LimbalEyes.value = CharacterBytes[13];
						CharacterDetails.EyeBrowType.value = CharacterBytes[14];
						CharacterDetails.LeftEye.value = CharacterBytes[15];
						CharacterDetails.Eye.value = CharacterBytes[16];
						CharacterDetails.Nose.value = CharacterBytes[17];
						CharacterDetails.Jaw.value = CharacterBytes[18];
						CharacterDetails.Lips.value = CharacterBytes[19];
						CharacterDetails.LipsTone.value = CharacterBytes[20];
						CharacterDetails.TailorMuscle.value = CharacterBytes[21];
						CharacterDetails.TailType.value = CharacterBytes[22];
						CharacterDetails.RBust.value = CharacterBytes[23];
						CharacterDetails.FacePaint.value = CharacterBytes[24];
						CharacterDetails.FacePaintColor.value = CharacterBytes[25];
						MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Race), CharacterBytes);
						if (charSaves.CharacterDetails.Height.value != 0.000)
						{
							CharacterDetails.Height.value = charSaves.CharacterDetails.Height.value;
							MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Body.Base, Settings.Instance.Character.Body.Height), "float", charSaves.CharacterDetails.Height.value.ToString());
						}
						CharacterDetails.Voices.value = charSaves.CharacterDetails.Voices.value;
						MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Voices), charSaves.CharacterDetails.Voices.GetBytes());
						CharacterDetails.MuscleTone.value = charSaves.CharacterDetails.MuscleTone.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Body.Base, Settings.Instance.Character.Body.MuscleTone), "float", charSaves.CharacterDetails.MuscleTone.value.ToString());
						CharacterDetails.TailSize.value = charSaves.CharacterDetails.TailSize.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Body.Base, Settings.Instance.Character.Body.TailSize), "float", charSaves.CharacterDetails.TailSize.value.ToString());
						CharacterDetails.BustX.value = charSaves.CharacterDetails.BustX.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Body.Base, Settings.Instance.Character.Body.Bust.Base, Settings.Instance.Character.Body.Bust.X), "float", charSaves.CharacterDetails.BustX.value.ToString());
						CharacterDetails.BustY.value = charSaves.CharacterDetails.BustY.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Body.Base, Settings.Instance.Character.Body.Bust.Base, Settings.Instance.Character.Body.Bust.Y), "float", charSaves.CharacterDetails.BustY.value.ToString());
						CharacterDetails.BustZ.value = charSaves.CharacterDetails.BustZ.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Body.Base, Settings.Instance.Character.Body.Bust.Base, Settings.Instance.Character.Body.Bust.Z), "float", charSaves.CharacterDetails.BustZ.value.ToString());
						CharacterDetails.HairRedPigment.value = charSaves.CharacterDetails.HairRedPigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HairRedPigment), "float", charSaves.CharacterDetails.HairRedPigment.value.ToString());
						CharacterDetails.HairBluePigment.value = charSaves.CharacterDetails.HairBluePigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HairBluePigment), "float", charSaves.CharacterDetails.HairBluePigment.value.ToString());
						CharacterDetails.HairGreenPigment.value = charSaves.CharacterDetails.HairGreenPigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HairGreenPigment), "float", charSaves.CharacterDetails.HairGreenPigment.value.ToString());
						CharacterDetails.HairGlowRed.value = charSaves.CharacterDetails.HairGlowRed.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HairGlowRed), "float", charSaves.CharacterDetails.HairGlowRed.value.ToString());
						CharacterDetails.HairGlowGreen.value = charSaves.CharacterDetails.HairGlowGreen.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HairGlowGreen), "float", charSaves.CharacterDetails.HairGlowGreen.value.ToString());
						CharacterDetails.HairGlowBlue.value = charSaves.CharacterDetails.HairGlowBlue.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HairGlowBlue), "float", charSaves.CharacterDetails.HairGlowBlue.value.ToString());
						CharacterDetails.HighlightRedPigment.value = charSaves.CharacterDetails.HighlightRedPigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HighlightRedPigment), "float", charSaves.CharacterDetails.HighlightRedPigment.value.ToString());
						CharacterDetails.HighlightGreenPigment.value = charSaves.CharacterDetails.HighlightGreenPigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HighlightGreenPigment), "float", charSaves.CharacterDetails.HighlightGreenPigment.value.ToString());
						CharacterDetails.HighlightBluePigment.value = charSaves.CharacterDetails.HighlightBluePigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HighlightBluePigment), "float", charSaves.CharacterDetails.HighlightBluePigment.value.ToString());
						CharacterDetails.SkinRedPigment.value = charSaves.CharacterDetails.SkinRedPigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.SkinRedPigment), "float", charSaves.CharacterDetails.SkinRedPigment.value.ToString());
						CharacterDetails.SkinGreenPigment.value = charSaves.CharacterDetails.SkinGreenPigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.SkinGreenPigment), "float", charSaves.CharacterDetails.SkinGreenPigment.value.ToString());
						CharacterDetails.SkinBluePigment.value = charSaves.CharacterDetails.SkinBluePigment.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.SkinBluePigment), "float", charSaves.CharacterDetails.SkinBluePigment.value.ToString());
						CharacterDetails.SkinRedGloss.value = charSaves.CharacterDetails.SkinRedGloss.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.SkinRedGloss), "float", charSaves.CharacterDetails.SkinRedGloss.value.ToString());
						CharacterDetails.SkinGreenGloss.value = charSaves.CharacterDetails.SkinGreenGloss.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.SkinGreenGloss), "float", charSaves.CharacterDetails.SkinGreenGloss.value.ToString());
						CharacterDetails.SkinBlueGloss.value = charSaves.CharacterDetails.SkinBlueGloss.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.SkinBlueGloss), "float", charSaves.CharacterDetails.SkinBlueGloss.value.ToString());
						CharacterDetails.LipsBrightness.value = charSaves.CharacterDetails.LipsBrightness.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LipsBrightness), "float", charSaves.CharacterDetails.LipsBrightness.value.ToString());
						CharacterDetails.LipsR.value = charSaves.CharacterDetails.LipsR.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LipsR), "float", charSaves.CharacterDetails.LipsR.value.ToString());
						CharacterDetails.LipsG.value = charSaves.CharacterDetails.LipsG.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LipsG), "float", charSaves.CharacterDetails.LipsG.value.ToString());
						CharacterDetails.LipsB.value = charSaves.CharacterDetails.LipsB.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LipsB), "float", charSaves.CharacterDetails.LipsB.value.ToString());
						CharacterDetails.LimbalR.value = charSaves.CharacterDetails.LimbalR.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LimbalR), "float", charSaves.CharacterDetails.LimbalR.value.ToString());
						CharacterDetails.LimbalG.value = charSaves.CharacterDetails.LimbalG.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LimbalG), "float", charSaves.CharacterDetails.LimbalG.value.ToString());
						CharacterDetails.LimbalB.value = charSaves.CharacterDetails.LimbalB.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LimbalB), "float", charSaves.CharacterDetails.LimbalB.value.ToString());
						CharacterDetails.LeftEyeRed.value = charSaves.CharacterDetails.LeftEyeRed.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LeftEyeRed), "float", charSaves.CharacterDetails.LeftEyeRed.value.ToString());
						CharacterDetails.LeftEyeGreen.value = charSaves.CharacterDetails.LeftEyeGreen.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LeftEyeGreen), "float", charSaves.CharacterDetails.LeftEyeGreen.value.ToString());
						CharacterDetails.LeftEyeBlue.value = charSaves.CharacterDetails.LeftEyeBlue.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.LeftEyeBlue), "float", charSaves.CharacterDetails.LeftEyeBlue.value.ToString());
						CharacterDetails.RightEyeRed.value = charSaves.CharacterDetails.RightEyeRed.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.RightEyeRed), "float", charSaves.CharacterDetails.RightEyeRed.value.ToString());
						CharacterDetails.RightEyeGreen.value = charSaves.CharacterDetails.RightEyeGreen.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.RightEyeGreen), "float", charSaves.CharacterDetails.RightEyeGreen.value.ToString());
						CharacterDetails.RightEyeBlue.value = charSaves.CharacterDetails.RightEyeBlue.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.RightEyeBlue), "float", charSaves.CharacterDetails.RightEyeBlue.value.ToString());
						if (CharacterDetails.MuscleTone.freezetest == true) { CharacterDetails.MuscleTone.freeze = true; CharacterDetails.MuscleTone.freezetest = false; }
						if (CharacterDetails.TailSize.freezetest == true) { CharacterDetails.TailSize.freeze = true; CharacterDetails.TailSize.freezetest = false; }
						if (CharacterDetails.BustX.freezetest == true) { CharacterDetails.BustX.freeze = true; CharacterDetails.BustX.freezetest = false; }
						if (CharacterDetails.BustY.freezetest == true) { CharacterDetails.BustY.freeze = true; CharacterDetails.BustY.freezetest = false; }
						if (CharacterDetails.BustZ.freezetest == true) { CharacterDetails.BustZ.freeze = true; CharacterDetails.BustZ.freezetest = false; }
						if (CharacterDetails.LipsBrightness.freezetest == true) { CharacterDetails.LipsBrightness.freeze = true; CharacterDetails.LipsBrightness.freezetest = false; }
						if (CharacterDetails.SkinBlueGloss.freezetest == true) { CharacterDetails.SkinBlueGloss.freeze = true; CharacterDetails.SkinBlueGloss.freezetest = false; }
						if (CharacterDetails.SkinGreenGloss.freezetest == true) { CharacterDetails.SkinGreenGloss.freeze = true; CharacterDetails.SkinGreenGloss.freezetest = false; }
						if (CharacterDetails.SkinRedGloss.freezetest == true) { CharacterDetails.SkinRedGloss.freeze = true; CharacterDetails.SkinRedGloss.freezetest = false; }
						if (CharacterDetails.SkinBluePigment.freezetest == true) { CharacterDetails.SkinBluePigment.freeze = true; CharacterDetails.SkinBluePigment.freezetest = false; }
						if (CharacterDetails.SkinGreenPigment.freezetest == true) { CharacterDetails.SkinGreenPigment.freeze = true; CharacterDetails.SkinGreenPigment.freezetest = false; }
						if (CharacterDetails.SkinRedPigment.freezetest == true) { CharacterDetails.SkinRedPigment.freeze = true; CharacterDetails.SkinRedPigment.freezetest = false; }
						if (CharacterDetails.HighlightBluePigment.freezetest == true) { CharacterDetails.HighlightBluePigment.freeze = true; CharacterDetails.HighlightBluePigment.freezetest = false; }
						if (CharacterDetails.HighlightGreenPigment.freezetest == true) { CharacterDetails.HighlightGreenPigment.freeze = true; CharacterDetails.HighlightGreenPigment.freezetest = false; }
						if (CharacterDetails.HighlightRedPigment.freezetest == true) { CharacterDetails.HighlightRedPigment.freeze = true; CharacterDetails.HighlightRedPigment.freezetest = false; }
						if (CharacterDetails.HairGlowBlue.freezetest == true) { CharacterDetails.HairGlowBlue.freeze = true; CharacterDetails.HairGlowBlue.freezetest = false; }
						if (CharacterDetails.HairGlowGreen.freezetest == true) { CharacterDetails.HairGlowGreen.freeze = true; CharacterDetails.HairGlowGreen.freezetest = false; }
						if (CharacterDetails.HairGlowRed.freezetest == true) { CharacterDetails.HairGlowRed.freeze = true; CharacterDetails.HairGlowRed.freezetest = false; }
						if (CharacterDetails.HairGreenPigment.freezetest == true) { CharacterDetails.HairGreenPigment.freeze = true; CharacterDetails.HairGreenPigment.freezetest = false; }
						if (CharacterDetails.HairBluePigment.freezetest == true) { CharacterDetails.HairBluePigment.freeze = true; CharacterDetails.HairBluePigment.freezetest = false; }
						if (CharacterDetails.HairRedPigment.freezetest == true) { CharacterDetails.HairRedPigment.freeze = true; CharacterDetails.HairRedPigment.freezetest = false; }
						if (CharacterDetails.Height.freezetest == true) { CharacterDetails.Height.freeze = true; CharacterDetails.Height.freezetest = false; }
						if (CharacterDetails.RightEyeBlue.freezetest == true) { CharacterDetails.RightEyeBlue.freeze = true; CharacterDetails.RightEyeBlue.freezetest = false; }
						if (CharacterDetails.RightEyeGreen.freezetest == true) { CharacterDetails.RightEyeGreen.freeze = true; CharacterDetails.RightEyeGreen.freezetest = false; }
						if (CharacterDetails.RightEyeRed.freezetest == true) { CharacterDetails.RightEyeRed.freeze = true; CharacterDetails.RightEyeRed.freezetest = false; }
						if (CharacterDetails.LeftEyeBlue.freezetest == true) { CharacterDetails.LeftEyeBlue.freeze = true; CharacterDetails.LeftEyeBlue.freezetest = false; }
						if (CharacterDetails.LeftEyeGreen.freezetest == true) { CharacterDetails.LeftEyeGreen.freeze = true; CharacterDetails.LeftEyeGreen.freezetest = false; }
						if (CharacterDetails.LeftEyeRed.freezetest == true) { CharacterDetails.LeftEyeRed.freeze = true; CharacterDetails.LeftEyeRed.freezetest = false; }
						if (CharacterDetails.LipsB.freezetest == true) { CharacterDetails.LipsB.freeze = true; CharacterDetails.LipsB.freezetest = false; }
						if (CharacterDetails.LipsG.freezetest == true) { CharacterDetails.LipsG.freeze = true; CharacterDetails.LipsG.freezetest = false; }
						if (CharacterDetails.LipsR.freezetest == true) { CharacterDetails.LipsR.freeze = true; CharacterDetails.LipsR.freezetest = false; }
						if (CharacterDetails.LimbalR.freezetest == true) { CharacterDetails.LimbalR.freeze = true; CharacterDetails.LimbalR.freezetest = false; }
						if (CharacterDetails.LimbalB.freezetest == true) { CharacterDetails.LimbalB.freeze = true; CharacterDetails.LimbalB.freezetest = false; }
						if (CharacterDetails.LimbalG.freezetest == true) { CharacterDetails.LimbalG.freeze = true; CharacterDetails.LimbalG.freezetest = false; }
					}
					if (savechoice == 0 || savechoice == 2)
					{
						byte[] EquipmentArray;
						EquipmentArray = MemoryManager.StringToByteArray(charSaves.EquipmentBytes.Replace(" ", string.Empty));
						CharacterDetails.HeadPiece.value = (EquipmentArray[0] + EquipmentArray[1] * 256);
						CharacterDetails.HeadV.value = EquipmentArray[2];
						CharacterDetails.HeadDye.value = EquipmentArray[3];
						CharacterDetails.Chest.value = (EquipmentArray[4] + EquipmentArray[5] * 256);
						CharacterDetails.ChestV.value = EquipmentArray[6];
						CharacterDetails.ChestDye.value = EquipmentArray[7];
						CharacterDetails.Arms.value = (EquipmentArray[8] + EquipmentArray[9] * 256);
						CharacterDetails.ArmsV.value = EquipmentArray[10];
						CharacterDetails.ArmsDye.value = EquipmentArray[11];
						CharacterDetails.Legs.value = (EquipmentArray[12] + EquipmentArray[13] * 256);
						CharacterDetails.LegsV.value = EquipmentArray[14];
						CharacterDetails.LegsDye.value = EquipmentArray[15];
						CharacterDetails.Feet.value = (EquipmentArray[16] + EquipmentArray[17] * 256);
						CharacterDetails.FeetVa.value = EquipmentArray[18];
						CharacterDetails.FeetDye.value = EquipmentArray[19];
						CharacterDetails.Ear.value = (EquipmentArray[20] + EquipmentArray[21] * 256);
						CharacterDetails.EarVa.value = EquipmentArray[22];
						CharacterDetails.Neck.value = (EquipmentArray[24] + EquipmentArray[25] * 256);
						CharacterDetails.NeckVa.value = EquipmentArray[26];
						CharacterDetails.Wrist.value = (EquipmentArray[28] + EquipmentArray[29] * 256);
						CharacterDetails.WristVa.value = EquipmentArray[30];
						CharacterDetails.RFinger.value = (EquipmentArray[32] + EquipmentArray[33] * 256);
						CharacterDetails.RFingerVa.value = EquipmentArray[34];
						CharacterDetails.LFinger.value = (EquipmentArray[36] + EquipmentArray[37] * 256);
						CharacterDetails.LFingerVa.value = EquipmentArray[38];
						MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HeadPiece), EquipmentArray);
						CharacterDetails.Job.value = charSaves.MainHand.Item1;
						CharacterDetails.WeaponBase.value = (byte)charSaves.MainHand.Item2;
						CharacterDetails.WeaponV.value = (byte)charSaves.MainHand.Item3;
						CharacterDetails.WeaponDye.value = (byte)charSaves.MainHand.Item4;
						MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Job), EquipmentFlyOut.WepTupleToByteAry(charSaves.MainHand));
						CharacterDetails.Offhand.value = charSaves.OffHand.Item1;
						CharacterDetails.OffhandBase.value = (byte)charSaves.OffHand.Item2;
						CharacterDetails.OffhandV.value = (byte)charSaves.OffHand.Item3;
						CharacterDetails.OffhandDye.value = (byte)charSaves.OffHand.Item4;
						MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Offhand), EquipmentFlyOut.WepTupleToByteAry(charSaves.OffHand));
						CharacterDetails.WeaponX.value = charSaves.CharacterDetails.WeaponX.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.WeaponX), "float", charSaves.CharacterDetails.WeaponX.value.ToString());
						CharacterDetails.WeaponY.value = charSaves.CharacterDetails.WeaponY.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.WeaponY), "float", charSaves.CharacterDetails.WeaponY.value.ToString());
						CharacterDetails.WeaponZ.value = charSaves.CharacterDetails.WeaponZ.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.WeaponZ), "float", charSaves.CharacterDetails.WeaponZ.value.ToString());
						CharacterDetails.WeaponRed.value = charSaves.CharacterDetails.WeaponRed.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.WeaponRed), "float", charSaves.CharacterDetails.WeaponRed.value.ToString());
						CharacterDetails.WeaponBlue.value = charSaves.CharacterDetails.WeaponBlue.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.WeaponBlue), "float", charSaves.CharacterDetails.WeaponBlue.value.ToString());
						CharacterDetails.WeaponGreen.value = charSaves.CharacterDetails.WeaponGreen.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.WeaponGreen), "float", charSaves.CharacterDetails.WeaponGreen.value.ToString());
						CharacterDetails.OffhandBlue.value = charSaves.CharacterDetails.OffhandBlue.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.OffhandBlue), "float", charSaves.CharacterDetails.OffhandBlue.value.ToString());
						CharacterDetails.OffhandGreen.value = charSaves.CharacterDetails.OffhandGreen.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.OffhandGreen), "float", charSaves.CharacterDetails.OffhandGreen.value.ToString());
						CharacterDetails.OffhandRed.value = charSaves.CharacterDetails.OffhandRed.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.OffhandRed), "float", charSaves.CharacterDetails.OffhandRed.value.ToString());
						CharacterDetails.OffhandX.value = charSaves.CharacterDetails.OffhandX.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.OffhandX), "float", charSaves.CharacterDetails.OffhandX.value.ToString());
						CharacterDetails.OffhandY.value = charSaves.CharacterDetails.OffhandY.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.OffhandY), "float", charSaves.CharacterDetails.OffhandY.value.ToString());
						CharacterDetails.OffhandZ.value = charSaves.CharacterDetails.OffhandZ.value;
						MemoryManager.Instance.MemLib.writeMemory(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.OffhandZ), "float", charSaves.CharacterDetails.OffhandZ.value.ToString());
						if (CharacterDetails.WeaponGreen.Cantbeused == true) { CharacterDetails.WeaponGreen.freeze = true; CharacterDetails.WeaponGreen.Cantbeused = false; }
						if (CharacterDetails.WeaponBlue.Cantbeused == true) { CharacterDetails.WeaponBlue.freeze = true; CharacterDetails.WeaponBlue.Cantbeused = false; }
						if (CharacterDetails.WeaponRed.Cantbeused == true) { CharacterDetails.WeaponRed.freeze = true; CharacterDetails.WeaponRed.Cantbeused = false; }
						if (CharacterDetails.WeaponZ.Cantbeused == true) { CharacterDetails.WeaponZ.freeze = true; CharacterDetails.WeaponZ.Cantbeused = false; }
						if (CharacterDetails.WeaponY.Cantbeused == true) { CharacterDetails.WeaponY.freeze = true; CharacterDetails.WeaponY.Cantbeused = false; }
						if (CharacterDetails.WeaponX.Cantbeused == true) { CharacterDetails.WeaponX.freeze = true; CharacterDetails.WeaponX.Cantbeused = false; }
						if (CharacterDetails.OffhandZ.Cantbeused == true) { CharacterDetails.OffhandZ.freeze = true; CharacterDetails.OffhandZ.Cantbeused = false; }
						if (CharacterDetails.OffhandY.Cantbeused == true) { CharacterDetails.OffhandY.freeze = true; CharacterDetails.OffhandY.Cantbeused = false; }
						if (CharacterDetails.OffhandX.Cantbeused == true) { CharacterDetails.OffhandX.freeze = true; CharacterDetails.OffhandX.Cantbeused = false; }
						if (CharacterDetails.OffhandRed.Cantbeused == true) { CharacterDetails.OffhandRed.freeze = true; CharacterDetails.OffhandRed.Cantbeused = false; }
						if (CharacterDetails.OffhandBlue.Cantbeused == true) { CharacterDetails.OffhandBlue.freeze = true; CharacterDetails.OffhandBlue.Cantbeused = false; }
						if (CharacterDetails.OffhandGreen.Cantbeused == true) { CharacterDetails.OffhandGreen.freeze = true; CharacterDetails.OffhandGreen.Cantbeused = false; }
					}
					Load.IsEnabled = true;
				}
			}
			catch (Exception exc)
			{
				MessageBox.Show("One or more fields were not formatted correctly.\n\n" + exc, " Error " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version, MessageBoxButton.OK, MessageBoxImage.Error);
				Load.IsEnabled = true;
			}
		}

		private void UnfreezeAll(object sender, RoutedEventArgs e)
		{
			CharacterDetails.TimeControl.freeze = false;
			CharacterDetails.Weather.freeze = false;
			CharacterDetails.CZoom.freeze = false;
			CharacterDetails.CameraYAMax.freeze = false;
			CharacterDetails.FOVC.freeze = false;
			CharacterDetails.CameraHeight2.freeze = false;
			CharacterDetails.CameraUpDown.freeze = false;
			CharacterDetails.CameraYAMin.freeze = false;
			CharacterDetails.CameraYAMax.freeze = false;
			CharacterDetails.Min.freeze = false;
			CharacterDetails.FOVMAX.freeze = false;
			CharacterDetails.Max.freeze = false;
			CharacterDetails.CamZ.freeze = false;
			CharacterDetails.CamY.freeze = false;
			CharacterDetails.CamX.freeze = false;
			CharacterDetails.EmoteSpeed1.freeze = false;
			CharacterDetails.Emote.freeze = false;
			CharacterDetails.MuscleTone.freeze = false;
			CharacterDetails.TailSize.freeze = false;
			CharacterDetails.LimbalEyes.freeze = false;
			CharacterDetails.BustX.freeze = false;
			CharacterDetails.BustY.freeze = false;
			CharacterDetails.BustZ.freeze = false;
			CharacterDetails.LipsBrightness.freeze = false;
			CharacterDetails.SkinBlueGloss.freeze = false;
			CharacterDetails.SkinGreenGloss.freeze = false;
			CharacterDetails.SkinRedGloss.freeze = false;
			CharacterDetails.SkinBluePigment.freeze = false;
			CharacterDetails.SkinGreenPigment.freeze = false;
			CharacterDetails.SkinRedPigment.freeze = false;
			CharacterDetails.HighlightBluePigment.freeze = false;
			CharacterDetails.HighlightGreenPigment.freeze = false;
			CharacterDetails.HighlightRedPigment.freeze = false;
			CharacterDetails.HairGlowBlue.freeze = false;
			CharacterDetails.HairGlowGreen.freeze = false;
			CharacterDetails.HairGlowRed.freeze = false;
			CharacterDetails.HairGreenPigment.freeze = false;
			CharacterDetails.HairBluePigment.freeze = false;
			CharacterDetails.HairRedPigment.freeze = false;
			CharacterDetails.Height.freeze = false;
			CharacterDetails.WeaponGreen.freeze = false;
			CharacterDetails.WeaponBlue.freeze = false;
			CharacterDetails.WeaponRed.freeze = false;
			CharacterDetails.WeaponZ.freeze = false;
			CharacterDetails.WeaponY.freeze = false;
			CharacterDetails.WeaponX.freeze = false;
			CharacterDetails.OffhandZ.freeze = false;
			CharacterDetails.OffhandY.freeze = false;
			CharacterDetails.OffhandX.freeze = false;
			CharacterDetails.OffhandRed.freeze = false;
			CharacterDetails.OffhandBlue.freeze = false;
			CharacterDetails.OffhandGreen.freeze = false;
			CharacterDetails.RightEyeBlue.freeze = false;
			CharacterDetails.RightEyeGreen.freeze = false;
			CharacterDetails.RightEyeRed.freeze = false;
			CharacterDetails.LeftEyeBlue.freeze = false;
			CharacterDetails.LeftEyeGreen.freeze = false;
			CharacterDetails.LeftEyeRed.freeze = false;
			CharacterDetails.LipsB.freeze = false;
			CharacterDetails.LipsG.freeze = false;
			CharacterDetails.LipsR.freeze = false;
			CharacterDetails.LimbalB.freeze = false;
			CharacterDetails.LimbalG.freeze = false;
			CharacterDetails.LimbalR.freeze = false;
			CharacterDetails.Race.freeze = false;
			CharacterDetails.Clan.freeze = false;
			CharacterDetails.Gender.freeze = false;
			CharacterDetails.Head.freeze = false;
			CharacterDetails.TailType.freeze = false;
			CharacterDetails.Nose.freeze = false;
			CharacterDetails.Lips.freeze = false;
			CharacterDetails.Voices.freeze = false;
			CharacterDetails.Hair.freeze = false;
			CharacterDetails.HairTone.freeze = false;
			CharacterDetails.HighlightTone.freeze = false;
			CharacterDetails.Jaw.freeze = false;
			CharacterDetails.RBust.freeze = false;
			CharacterDetails.RHeight.freeze = false;
			CharacterDetails.LipsTone.freeze = false;
			CharacterDetails.Skintone.freeze = false;
			CharacterDetails.FacialFeatures.freeze = false;
			CharacterDetails.TailorMuscle.freeze = false;
			CharacterDetails.Eye.freeze = false;
			CharacterDetails.RightEye.freeze = false;
			CharacterDetails.EyeBrowType.freeze = false;
			CharacterDetails.LeftEye.freeze = false;
			CharacterDetails.Offhand.freeze = false;
			CharacterDetails.FacePaint.freeze = false;
			CharacterDetails.FacePaintColor.freeze = false;
			CharacterDetails.Job.freeze = false;
			CharacterDetails.HeadPiece.freeze = false;
			CharacterDetails.Chest.freeze = false;
			CharacterDetails.Arms.freeze = false;
			CharacterDetails.Legs.freeze = false;
			CharacterDetails.Feet.freeze = false;
			CharacterDetails.Ear.freeze = false;
			CharacterDetails.Neck.freeze = false;
			CharacterDetails.Wrist.freeze = false;
			CharacterDetails.Highlights.freeze = false;
			CharacterDetails.RFinger.freeze = false;
			CharacterDetails.LFinger.freeze = false;
			CharacterDetails.ScaleX.freeze = false;
			CharacterDetails.ScaleY.freeze = false;
			CharacterDetails.ScaleZ.freeze = false;
			CharacterDetails.Transparency.freeze = false;
			CharacterDetails.ModelType.freeze = false;
			CharacterDetails.TestArray.freeze = false;
			CharacterDetails.TestArray2.freeze = false;
			CharacterDetails.BodyType.freeze = false;
			CharacterDetails.X.freeze = false;
			CharacterDetails.Y.freeze = false;
			CharacterDetails.Z.freeze = false;
			CharacterDetails.Rotation.freeze = false;
			CharacterDetails.Rotation2.freeze = false;
			CharacterDetails.Rotation3.freeze = false;
			CharacterDetails.Rotation4.freeze = false;
			CharacterDetails.FCTag.freeze = false;
			CharacterDetails.Title.freeze = false;
			CharacterDetails.JobIco.freeze = false;
			CharacterDetails.EmoteOld.freeze = false;
			CharacterDetails.EntityType.freeze = false;
			CharacterDetails.DataPath.freeze = false;
			CharacterDetails.RotateFreeze = false;
			CharacterDetailsView.xyzcheck = false;
		}

		private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
		{
			if (SaveSettings.Default.TopApp == false)
			{
				SaveSettings.Default.TopApp = true;
				//   Properties.Settings.Default.Save();
				Topmost = true;
				(DataContext as MainViewModel).ToggleStatus(true);
			}
			else
			{
				SaveSettings.Default.TopApp = false;
				//    Properties.Settings.Default.Save();
				Topmost = false;
				(DataContext as MainViewModel).ToggleStatus(false);
			}
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateProgram();
		}

		private void GposeButton_Checked(object sender, RoutedEventArgs e)
		{
			CharacterRefreshButton.IsEnabled = false;

			MainViewModel.ViewTime.CamXCheck.IsEnabled = true;
			MainViewModel.ViewTime.CamYCheck.IsEnabled = true;
			MainViewModel.ViewTime.CamZCheck.IsEnabled = true;

			MainViewModel.ViewTime.HairSelectButton.IsEnabled = false;
			MainViewModel.ViewTime.ModelTypeButton.IsEnabled = false;
			MainViewModel.ViewTime.HighlightcolorSearch.IsEnabled = false;
			MainViewModel.ViewTime.LeftEyeSearch.IsEnabled = false;
			MainViewModel.ViewTime.LimbalEyeSearch.IsEnabled = false;
			MainViewModel.ViewTime.RightEyeSearch.IsEnabled = false;
			MainViewModel.ViewTime.SkinSearch.IsEnabled = false;
			MainViewModel.ViewTime.FacePaint_Color.IsEnabled = false;
			MainViewModel.ViewTime.FacePaint_Color_Copy.IsEnabled = false;
			MainViewModel.ViewTime.FacialFeature.IsEnabled = false;
			MainViewModel.ViewTime.LipColorSearch.IsEnabled = false;
			MainViewModel.ViewTime.HairColorSearch.IsEnabled = false;
			MainViewModel.ViewTime.SpecialControl.IsOpen = false;
			MainViewModel.ViewTime.SpecialControl.AnimatedTabControl.SelectedIndex = -1;

			MainViewModel.ViewTime2.BodySearch.IsEnabled = false;
			MainViewModel.ViewTime2.EarSearch.IsEnabled = false;
			MainViewModel.ViewTime2.FeetSearch.IsEnabled = false;
			MainViewModel.ViewTime2.HandSearch.IsEnabled = false;
			MainViewModel.ViewTime2.HeadSearch.IsEnabled = false;
			MainViewModel.ViewTime2.LeftSearch.IsEnabled = false;
			MainViewModel.ViewTime2.LegsSearch.IsEnabled = false;
			MainViewModel.ViewTime2.MainSearch.IsEnabled = false;
			MainViewModel.ViewTime2.NeckSearch.IsEnabled = false;
			MainViewModel.ViewTime2.OffSearch.IsEnabled = false;
			MainViewModel.ViewTime2.PropSearch.IsEnabled = false;
			MainViewModel.ViewTime2.PropSearchOH.IsEnabled = false;
			MainViewModel.ViewTime2.RightSearch.IsEnabled = false;
			MainViewModel.ViewTime2.WristSearch.IsEnabled = false;
			MainViewModel.ViewTime2.NPC_Click.IsEnabled = false;
			MainViewModel.ViewTime2.EquipmentControl.IsOpen = false;
			MainViewModel.ViewTime2.EquipmentControl.AnimatedTabControl.SelectedIndex = -1;

			MainViewModel.ViewTime4.StatusEffectBox.IsReadOnly = false;
			MainViewModel.ViewTime4.StatusEffectBox2.IsReadOnly = false;
			MainViewModel.ViewTime4.StatusEffectZero.IsEnabled = true;
			MainViewModel.ViewTime4.StatusEffectText.IsEnabled = true;

			if (TargetButton.IsEnabled == true)
				CharacterDetailsViewModel.baseAddr = MemoryManager.Instance.GposeAddress;
			if (TargetButton.IsEnabled == false)
				CharacterDetailsViewModel.baseAddr = MemoryManager.Instance.GposeEntityOffset;
		}

		private void GposeButton_Unchecked(object sender, RoutedEventArgs e)
		{
			CharacterRefreshButton.IsEnabled = true;

			MainViewModel.ViewTime.CamXCheck.IsEnabled = false;
			MainViewModel.ViewTime.CamYCheck.IsEnabled = false;
			MainViewModel.ViewTime.CamZCheck.IsEnabled = false;

			MainViewModel.ViewTime.HairSelectButton.IsEnabled = true;
			MainViewModel.ViewTime.ModelTypeButton.IsEnabled = true;
			MainViewModel.ViewTime.HighlightcolorSearch.IsEnabled = true;
			MainViewModel.ViewTime.LeftEyeSearch.IsEnabled = true;
			MainViewModel.ViewTime.LimbalEyeSearch.IsEnabled = true;
			MainViewModel.ViewTime.RightEyeSearch.IsEnabled = true;
			MainViewModel.ViewTime.SkinSearch.IsEnabled = true;
			MainViewModel.ViewTime.FacePaint_Color.IsEnabled = true;
			MainViewModel.ViewTime.FacePaint_Color_Copy.IsEnabled = true;
			MainViewModel.ViewTime.FacialFeature.IsEnabled = true;
			MainViewModel.ViewTime.LipColorSearch.IsEnabled = true;
			MainViewModel.ViewTime.HairColorSearch.IsEnabled = true;
			MainViewModel.ViewTime2.BodySearch.IsEnabled = true;
			MainViewModel.ViewTime2.EarSearch.IsEnabled = true;
			MainViewModel.ViewTime2.FeetSearch.IsEnabled = true;
			MainViewModel.ViewTime2.HandSearch.IsEnabled = true;
			MainViewModel.ViewTime2.HeadSearch.IsEnabled = true;
			MainViewModel.ViewTime2.LeftSearch.IsEnabled = true;
			MainViewModel.ViewTime2.LegsSearch.IsEnabled = true;
			MainViewModel.ViewTime2.MainSearch.IsEnabled = true;
			MainViewModel.ViewTime2.NeckSearch.IsEnabled = true;
			MainViewModel.ViewTime2.OffSearch.IsEnabled = true;
			MainViewModel.ViewTime2.PropSearch.IsEnabled = true;
			MainViewModel.ViewTime2.PropSearchOH.IsEnabled = true;
			MainViewModel.ViewTime2.RightSearch.IsEnabled = true;
			MainViewModel.ViewTime2.WristSearch.IsEnabled = true;
			MainViewModel.ViewTime2.NPC_Click.IsEnabled = true;

			MainViewModel.ViewTime4.StatusEffectBox.IsReadOnly = true;
			MainViewModel.ViewTime4.StatusEffectBox2.IsReadOnly = true;
			MainViewModel.ViewTime4.StatusEffectZero.IsEnabled = false;
			MainViewModel.ViewTime4.StatusEffectText.IsEnabled = false;

			if (GposeButton.IsKeyboardFocusWithin || GposeButton.IsMouseOver)
				CharacterDetailsViewModel.baseAddr = MemoryManager.Add(MemoryManager.Instance.BaseAddress, CharacterDetailsViewModel.eOffset);
		}

		private void TargetButton_Checked(object sender, RoutedEventArgs e)
		{
			CharacterRefreshButton.IsEnabled = false;
			if (GposeButton.IsEnabled == false)
				CharacterDetailsViewModel.baseAddr = MemoryManager.Instance.TargetAddress;
			if (GposeButton.IsEnabled == true)
				CharacterDetailsViewModel.baseAddr = MemoryManager.Instance.GposeAddress;
		}

		private void TargetButton_Unchecked(object sender, RoutedEventArgs e)
		{
			CharacterRefreshButton.IsEnabled = true;
			if (TargetButton.IsKeyboardFocusWithin || TargetButton.IsMouseOver)
			{
				if (GposeButton.IsEnabled == false)
					CharacterDetailsViewModel.baseAddr = MemoryManager.Add(MemoryManager.Instance.BaseAddress, CharacterDetailsViewModel.eOffset);
				if (GposeButton.IsEnabled == true)
					CharacterDetailsViewModel.baseAddr = MemoryManager.Instance.GposeEntityOffset;
			}
		}

		private void DiscordButton_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://discord.gg/hq3DnBa");
		}

		private void SavePoint_Click(object sender, RoutedEventArgs e)
		{
			SaveSettings.Default.MainHandQuads = new WepTuple(CharacterDetails.Job.value, CharacterDetails.WeaponBase.value, CharacterDetails.WeaponV.value, CharacterDetails.WeaponDye.value);
			SaveSettings.Default.OffHandQuads = new WepTuple(CharacterDetails.Offhand.value, CharacterDetails.OffhandBase.value, CharacterDetails.OffhandV.value, CharacterDetails.OffhandDye.value);
			SaveSettings.Default.EquipmentBytes = CharacterDetails.TestArray2.value;
			SaveSettings.Default.CharacterAoBBytes = CharacterDetails.TestArray.value;
			MessageBox.Show($"Main Hand Values: {CharacterDetails.Job.value},{CharacterDetails.WeaponBase.value},{CharacterDetails.WeaponV.value},{CharacterDetails.WeaponDye.value}" + Environment.NewLine +
				$"Off Hand Values: {CharacterDetails.Offhand.value},{CharacterDetails.OffhandBase.value},{CharacterDetails.OffhandV.value},{CharacterDetails.OffhandDye.value}" + Environment.NewLine +
				$"Equipment AoB {CharacterDetails.TestArray2.value}" + Environment.NewLine + $"Character AoB {CharacterDetails.TestArray.value}", "Save Point Made!");
		}

		private void LoadSavePoint_Click(object sender, RoutedEventArgs e)
		{
			if (SaveSettings.Default.CharacterAoBBytes.Length <= 0) return;
			CharacterDetails.Race.freeze = true;
			CharacterDetails.Clan.freeze = true;
			CharacterDetails.Gender.freeze = true;
			CharacterDetails.Head.freeze = true;
			CharacterDetails.TailType.freeze = true;
			CharacterDetails.LimbalEyes.freeze = true;
			CharacterDetails.Nose.freeze = true;
			CharacterDetails.Lips.freeze = true;
			CharacterDetails.BodyType.freeze = true;
			CharacterDetails.Highlights.freeze = true;
			CharacterDetails.Hair.freeze = true;
			CharacterDetails.HairTone.freeze = true;
			CharacterDetails.HighlightTone.freeze = true;
			CharacterDetails.Jaw.freeze = true;
			CharacterDetails.RBust.freeze = true;
			CharacterDetails.RHeight.freeze = true;
			CharacterDetails.LipsTone.freeze = true;
			CharacterDetails.Skintone.freeze = true;
			CharacterDetails.FacialFeatures.freeze = true;
			CharacterDetails.TailorMuscle.freeze = true;
			CharacterDetails.Eye.freeze = true;
			CharacterDetails.RightEye.freeze = true;
			CharacterDetails.EyeBrowType.freeze = true;
			CharacterDetails.LeftEye.freeze = true;
			CharacterDetails.FacePaint.freeze = true;
			CharacterDetails.FacePaintColor.freeze = true;
			CharacterDetails.Offhand.freeze = true;
			CharacterDetails.Job.freeze = true;
			CharacterDetails.HeadPiece.freeze = true;
			CharacterDetails.Chest.freeze = true;
			CharacterDetails.Arms.freeze = true;
			CharacterDetails.Legs.freeze = true;
			CharacterDetails.Feet.freeze = true;
			CharacterDetails.Ear.freeze = true;
			CharacterDetails.Neck.freeze = true;
			CharacterDetails.Wrist.freeze = true;
			CharacterDetails.RFinger.freeze = true;
			CharacterDetails.LFinger.freeze = true;
			byte[] CharacterBytes;
			CharacterBytes = MemoryManager.StringToByteArray(SaveSettings.Default.CharacterAoBBytes.Replace(" ", string.Empty));
			CharacterDetails.Race.value = CharacterBytes[0];
			CharacterDetails.Gender.value = CharacterBytes[1];
			CharacterDetails.BodyType.value = CharacterBytes[2];
			CharacterDetails.RHeight.value = CharacterBytes[3];
			CharacterDetails.Clan.value = CharacterBytes[4];
			CharacterDetails.Head.value = CharacterBytes[5];
			CharacterDetails.Hair.value = CharacterBytes[6];
			CharacterDetails.Highlights.value = CharacterBytes[7];
			CharacterDetails.Skintone.value = CharacterBytes[8];
			CharacterDetails.RightEye.value = CharacterBytes[9];
			CharacterDetails.HairTone.value = CharacterBytes[10];
			CharacterDetails.HighlightTone.value = CharacterBytes[11];
			CharacterDetails.FacialFeatures.value = CharacterBytes[12];
			CharacterDetails.LimbalEyes.value = CharacterBytes[13];
			CharacterDetails.EyeBrowType.value = CharacterBytes[14];
			CharacterDetails.LeftEye.value = CharacterBytes[15];
			CharacterDetails.Eye.value = CharacterBytes[16];
			CharacterDetails.Nose.value = CharacterBytes[17];
			CharacterDetails.Jaw.value = CharacterBytes[18];
			CharacterDetails.Lips.value = CharacterBytes[19];
			CharacterDetails.LipsTone.value = CharacterBytes[20];
			CharacterDetails.TailorMuscle.value = CharacterBytes[21];
			CharacterDetails.TailType.value = CharacterBytes[22];
			CharacterDetails.RBust.value = CharacterBytes[23];
			CharacterDetails.FacePaint.value = CharacterBytes[24];
			CharacterDetails.FacePaintColor.value = CharacterBytes[25];
			MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Race), CharacterBytes);
			byte[] EquipmentArray;
			EquipmentArray = MemoryManager.StringToByteArray(SaveSettings.Default.EquipmentBytes.Replace(" ", string.Empty));
			CharacterDetails.HeadPiece.value = (EquipmentArray[0] + EquipmentArray[1] * 256);
			CharacterDetails.HeadV.value = EquipmentArray[2];
			CharacterDetails.HeadDye.value = EquipmentArray[3];
			CharacterDetails.Chest.value = (EquipmentArray[4] + EquipmentArray[5] * 256);
			CharacterDetails.ChestV.value = EquipmentArray[6];
			CharacterDetails.ChestDye.value = EquipmentArray[7];
			CharacterDetails.Arms.value = (EquipmentArray[8] + EquipmentArray[9] * 256);
			CharacterDetails.ArmsV.value = EquipmentArray[10];
			CharacterDetails.ArmsDye.value = EquipmentArray[11];
			CharacterDetails.Legs.value = (EquipmentArray[12] + EquipmentArray[13] * 256);
			CharacterDetails.LegsV.value = EquipmentArray[14];
			CharacterDetails.LegsDye.value = EquipmentArray[15];
			CharacterDetails.Feet.value = (EquipmentArray[16] + EquipmentArray[17] * 256);
			CharacterDetails.FeetVa.value = EquipmentArray[18];
			CharacterDetails.FeetDye.value = EquipmentArray[19];
			CharacterDetails.Ear.value = (EquipmentArray[20] + EquipmentArray[21] * 256);
			CharacterDetails.EarVa.value = EquipmentArray[22];
			CharacterDetails.Neck.value = (EquipmentArray[24] + EquipmentArray[25] * 256);
			CharacterDetails.NeckVa.value = EquipmentArray[26];
			CharacterDetails.Wrist.value = (EquipmentArray[28] + EquipmentArray[29] * 256);
			CharacterDetails.WristVa.value = EquipmentArray[30];
			CharacterDetails.RFinger.value = (EquipmentArray[32] + EquipmentArray[33] * 256);
			CharacterDetails.RFingerVa.value = EquipmentArray[34];
			CharacterDetails.LFinger.value = (EquipmentArray[36] + EquipmentArray[37] * 256);
			CharacterDetails.LFingerVa.value = EquipmentArray[38];
			MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.HeadPiece), EquipmentArray);
			CharacterDetails.Job.value = SaveSettings.Default.MainHandQuads.Item1;
			CharacterDetails.WeaponBase.value = SaveSettings.Default.MainHandQuads.Item2;
			CharacterDetails.WeaponV.value = (byte)SaveSettings.Default.MainHandQuads.Item3;
			CharacterDetails.WeaponDye.value = (byte)SaveSettings.Default.MainHandQuads.Item4;
			MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Job), EquipmentFlyOut.WepTupleToByteAry(SaveSettings.Default.MainHandQuads));
			CharacterDetails.Offhand.value = SaveSettings.Default.OffHandQuads.Item1;
			CharacterDetails.OffhandBase.value = SaveSettings.Default.OffHandQuads.Item2;
			CharacterDetails.OffhandV.value = (byte)SaveSettings.Default.OffHandQuads.Item3;
			CharacterDetails.OffhandDye.value = (byte)SaveSettings.Default.OffHandQuads.Item4;
			MemoryManager.Instance.MemLib.writeBytes(MemoryManager.GetAddressString(CharacterDetailsViewModel.baseAddr, Settings.Instance.Character.Offhand), EquipmentFlyOut.WepTupleToByteAry(SaveSettings.Default.OffHandQuads));
		}
	}
}
