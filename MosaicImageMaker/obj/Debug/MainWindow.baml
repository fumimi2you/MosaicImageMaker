   M S B A M L   `   `   `  ����  MosaicImageMaker    WpfAppSample.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      -   5          �Name$  
mainWindow��5   	    � Title$ WpfAppSample��5      
���   � J� CommandBindings 5      )  #System.Windows.Input.CommandBinding  5   
   -   5           Command$ ApplicationCommands.Open��5      5        5	   
   -   5        $  ApplicationCommands.SaveAs��5
      5        5   
   -   5        $ ApplicationCommands.Close��5      5      .��5      �  � ColumnDefinitions 5   
   �� 5       � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      .+�5   
   o� -   5         o�Name$ mainMenu��5      $��0��6!   $��0��61   .@�5      m� -   5         m�Name$ fileMenuItem��5      $��ファイル(_F)��6-   .@�5      m� -   5        $ openMenuItem��5      $��
開く(_O)��61    m� Command$ ApplicationCommands.Open��5      m� 5      -   5        $ saveAsMenuItem��5      $"��名前を付けて保存(_A)��63   $  ApplicationCommands.SaveAs��5      �� 5      m� 5       -	   5        $ exitMenuItem��5       $��
終了(_X)��61   $ ApplicationCommands.Close��5!      	 m� InputGestureText$	 Alt+F4��5"      5#      5$      �� 5%   
   -
   5        
 ��Name$
 scrollViewer��5%      $��0��6-   $��1��6=   $��Disabled=�5&      .��5'      �� -   5         ��Name$ image��5'      $��Uniform=�6#    �� BitmapScalingMode$ HighQuality=�5(      5)      5*      5+      