"""import pandas as pd
import numpy as np
#from sklearn.model_selection import train_test_split
#from sklearn.neural_network import MLPClassifier
import csv
import time

st = time.time()

# Load the data into a pandas DataFrame
df = pd.read_json("heroes.json")
# df = df.transpose()

# Use the get_dummies function to perform one-hot encoding
df_encoded = pd.get_dummies(df, columns=['name'])

# Print the resulting DataFrame
for column in df_encoded:
    for index, row in df_encoded.iterrows():
        print(f"{row[column]:>3}", end=" ")
    print(column)


print(time.time() - st)"""
import pickle
import numpy as np
import pandas as pd
from sklearn.neural_network import MLPClassifier
from sklearn.model_selection import train_test_split

# Load the data from the CSV file
# df = pd.read_csv("sortedDataGameMode22.csv", header=None)
dfRad = pd.read_csv("duoDataRadiant.csv", header=None)
dfDir = pd.read_csv("duoDataDire.csv", header=None)

dfRad = dfRad.values
dfDir = dfDir.values
X_rad = dfRad[:, :2]
y_rad = dfRad[:, 2]
X_dir = dfDir[:, :2]
y_dir = dfDir[:, 2]
# for i in range(0, 10):
#     if X is None:
#         X = df[:1000, i:1 + i]
#         y = df[:1000, 10]
#     elif i < 5:
#         X = np.vstack((X, df[:1000, i:1 + i]))
#         y = np.hstack((y, df[:1000, 10]))
#     else:
#         X = np.vstack((X, df[:1000, i:1 + i] + 130))
#         y = np.hstack((y, df[:1000, 10]))


# Split the data into training and test sets
X_train, X_test, y_train, y_test = train_test_split(X_dir, y_dir, test_size=0.2)

# Define the network architecture
#mlp = MLPClassifier(hidden_layer_sizes=(128, 128), max_iter=1500, activation='relu', solver='sgd', verbose=True, tol=1e-4, random_state=100, learning_rate_init=.01)

# Train the model

# Evaluate the model on the test data
#accuracy = mlp.score(X_test, y_test)
#print(accuracy)
#pickle.dump(mlp, open('finalized_modelRadiant.sav', 'wb'))
mlp_rad = pickle.load(open('finalized_modelDire.sav', 'rb'))
mlp_dir = pickle.load(open('finalized_modelRadiant.sav', 'rb'))
mlp_dir.fit(X_train, y_train)


team = 1 #if int(input("Input 1 if Dire, somthing else if not")) == 1 else 0

heroes = [int(input())]
for j in range(4):
    bestChoice = 0
    bestWinRate = 0.
    if team == 0:
        for i in range(131, 130*2):
            tmp = 0
            k = 0
            for hero in heroes:
                if i in heroes:
                    k = 0
                    break
                tmp1 = mlp_rad.predict([[int(hero)+130, i]])
                tmp += float(tmp1[0])
                k += 1
            if k == 0:
                continue
            if tmp/k > bestWinRate:
                bestWinRate = tmp/k
                bestChoice = i-131
    else:
        for i in range(1, 130):
            tmp = 0
            k = 0
            for hero in heroes:
                if i in heroes:
                    k = 0
                    break
                tmp1 = mlp_dir.predict([[int(hero), i]])
                tmp += float(tmp1[0])
                k += 1
            if k == 0:
                continue
            if tmp/k > bestWinRate:
                bestWinRate = tmp/k
                bestChoice = i
    print(f"The best hero choice is {bestChoice}")
    print("Whats your next  hero")
    heroes.append(int(input()))

# result = mlp.score(X_test, y_test)
# print(result)
