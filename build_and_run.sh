BALDI_DIRECTORY="/mnt/niko/SteamLibrary/steamapps/common/Baldi's Basics Plus/"
dotnet build --configuration Debug

cd "$BALDI_DIRECTORY"
exec "$BALDI_DIRECTORY/run_bepinex.sh"
return 0