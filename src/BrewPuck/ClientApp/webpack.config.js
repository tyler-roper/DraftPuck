const path = require('path');
const webpack = require('webpack');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const VueLoaderPlugin = require('vue-loader/lib/plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const CopyPlugin = require('copy-webpack-plugin');
// __dirname is ClientApp here.
const buildPath = path.resolve(__dirname, '../wwwroot', 'dist');
const entryPath = path.resolve(__dirname, 'src', 'main');
const assetsPathFileGlob = path.resolve(__dirname, 'src', 'assets/**/*');
const viewTemplatePath = path.resolve(__dirname, '../Views', 'App', 'IndexTemplate.cshtml');
const viewFilePath = path.resolve(__dirname, '../Views', 'App', 'Index.cshtml');
const configProvider = (env) => {
    const isDev = env !== 'production';
    const config = {
        entry: {
            app: entryPath
        },
        mode: isDev ? 'development' : 'production',
        module: {
            rules: [
                { test: /\.ts|vue$/, loader: "eslint-loader", exclude: "/node_modules/", enforce: "pre", options: { emitWarning: true, emitError: true, configFile: "./.eslintrc.js" } },
                { test: /\.ts$/, use: ["babel-loader", { loader: "ts-loader", options: { transpileOnly: true, appendTsSuffixTo: [/\.vue$/] } }] },
                { test: /\.css$/, use: [isDev ? 'style-loader' : MiniCssExtractPlugin.loader, { loader: 'css-loader', options: { url: true } }] },
                { test: /\.less$/, use: [isDev ? 'style-loader' : MiniCssExtractPlugin.loader, { loader: 'css-loader', options: { url: true } }, "less-loader"] },
                {
                    test: /\.scss/, use: [isDev ? 'style-loader' : MiniCssExtractPlugin.loader, { loader: 'css-loader', options: { url: true } }, {
                        loader: "sass-loader", options: { additionalData: `
                            @import '@/assets/scss/variables.scss';
                        ` }
                    }]
                },
                { test: /\.vue$/, use: ["vue-loader"] },
                { test: /\.(png|jpg|jpeg|gif|svg|ttf|eot|woff|woff2)$/, use: [{ loader: "url-loader", options: { esModule: false, limit: 25000 } }] }
            ]
        },
        optimization: {
            noEmitOnErrors: true,
            minimize: isDev ? false : true,
            splitChunks: {
                chunks: 'all',
                automaticNameDelimiter: '-'
            }
        },
        devServer: {
            proxy: {
                '*': {
                    target: 'https://localhost:17000',
                    secure: false
                }
            },
            port: 17010,
            host: 'localhost',
            hot: true,
            https: true
        },
        resolve: {
            alias: {
                '@': path.join(__dirname, "src"),
                'src': path.join(__dirname, "src")
            },
            extensions: [".vue", ".ts", ".js", ".less", ".css"],
            modules: ["node_modules", "lib"]
        },
        output: {
            filename: isDev ? "[name]-dev.js" : "[name].[hash].js",
            chunkFilename: isDev ? "[name]-dev.chunk.js" : "[name].[hash].chunk.js",
            path: buildPath,
            publicPath: "" //UN-COMMENT FOR NPM RUN BUILD
            //publicPath: "/dist/" //COMMENT FOR NPM RUN BUILD
        },
        plugins: [
            new webpack.ProgressPlugin(),
            new CleanWebpackPlugin(),
            new VueLoaderPlugin(),
            new MiniCssExtractPlugin({
                filename: isDev ? "[name]-dev.css" : "[name].[hash].css",
                chunkFilename: isDev ? "[name]-dev.chunk.css" : "[name].[hash].chunk.css"
            }),
            new HtmlWebpackPlugin({
                template: viewTemplatePath,
                //filename: viewFilePath,  //COMMENT FOR NPM RUN BUILD
                minify: false,
                title: "BREWPUCK"
            }),
            new CopyPlugin([{
                from: assetsPathFileGlob,
                to: buildPath + '/assets',
                flatten: true
            }])
        ]
    };
    if (isDev) {
        config.devtool = 'cheap-module-eval-source-map';
        config.devServer.https = {
            pfx: path.resolve(process.env.USERPROFILE, '.aspnet/https/BrewPuck.pfx'),
            passphrase: 'b30b3f16-18c1-4a00-8f8f-0ce0fba61cb2'
        }
    }
    return config;
};
module.exports = configProvider;