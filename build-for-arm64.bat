docker buildx create --name mybuilder --use
docker buildx build --platform linux/arm64 -f "C:\Users\the_a\source\repos\Uwuify.DiscordBot\Uwuify.DiscordBot.WorkerService\Dockerfile" --force-rm -t nickmartin1ee7/uwuifydiscordbotworkerservice:latest --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=Uwuify.DiscordBot.WorkerService" --load "C:\Users\the_a\source\repos\Uwuify.DiscordBot"
docker login
docker push nickmartin1ee7/uwuifydiscordbotworkerservice:latest