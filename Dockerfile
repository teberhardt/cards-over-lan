FROM mono:latest
EXPOSE 80

WORKDIR /usr/src/app/build

Copy CardsOverLan/ CardsOverLan/ 
Copy packs/ packs/
Copy web_content/ web_content/
Copy CardsOverLan.sln .

RUN nuget restore CardsOverLan.sln
RUN msbuild CardsOverLan.sln

RUN cp -r CardsOverLan/bin/Debug /usr/src/app/CardsOverLan

WORKDIR /usr/src/app/CardsOverLan

RUN rm -rf /usr/src/app/build

VOLUME /usr/src/app/CardsOverLan/packs
VOLUME /usr/src/app/CardsOverLan/packs/settings.json

ENTRYPOINT ["mono", "CardsOverLan.exe"]
